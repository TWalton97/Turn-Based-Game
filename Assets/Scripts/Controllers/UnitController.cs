using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;
using System.Linq;
using Unity.Netcode.Components;
using UnityEditor.PackageManager;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

public class UnitController : NetworkBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public string UnitName;

    public UnitAttributes UnitStats;
    public CombatStats CombatStats;

    public ClassStatPresetSO UnitData;
    public int MaxHealth = 30;
    public NetworkVariable<int> CurrentHealth;
    public int DisplayedHealth;

    public int MaxMana = 5;
    public NetworkVariable<int> CurrentMana;
    public int DisplayedMana;

    public NetworkVariable<bool> ServerIsAlive;
    public bool ClientIsAlive;

    public bool ActionPhaseStarted = false;

    public Team UnitTeam;

    public List<BaseAbility> BaseAbilities;
    public List<RuntimeAbilityInstance> RuntimeAbilityInstances;

    private MeshRenderer meshRenderer;
    private Material mat;

    private Color originalEmission;

    [SerializeField] private Color highlightEmission = Color.white * 2f;

    public Action OnDisplayedManaChanged;
    public Action OnDisplayedHealthChanged;
    public Action OnClientDie;
    public Action OnServerDie;

    public EnemyController enemyController { get; private set; }
    public StatusEffectController statusEffectController { get; private set; }
    private Vector3 startPos;

    public List<StatModifier> StatModifiers;
    public Dictionary<StatType, float> CachedStats;
    public bool CachedStatsDirty = true;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        mat = meshRenderer.material;
        originalEmission = mat.GetColor("_EmissionColor");

        enemyController = GetComponent<EnemyController>();
        statusEffectController = GetComponent<StatusEffectController>();

        TurnManager.OnServerTurnStarted += ServerTurnInitialization;
        TurnManager.OnServerActionPhaseStarted += ServerBeginActionPhase;
        TurnManager.OnServerTurnEnded += ServerEndTurn;

        TurnManager.OnClientTurnStarted += ClientTurnInitialization;
        TurnManager.OnClientActionPhaseStarted += ClientBeginActionPhase;
        TurnManager.OnClientTurnEnded += ClientEndTurn;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ServerIsAlive.Value = true;
        ClientIsAlive = true;

        BattleManager.instance.RegisterUnit(this);

        for (int i = 0; i < BaseAbilities.Count; i++)
        {
            RuntimeAbilityInstance runtimeAbilityInstance = new();
            runtimeAbilityInstance.Ability = BaseAbilities[i];
            RuntimeAbilityInstances.Add(runtimeAbilityInstance);
        }

        ApplyClassPresetStats();
        RecalculateAllStats();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        BattleManager.instance.UnregisterUnit(this);
    }

    public void SyncManaValues(UnitController controller)
    {
        DisplayedMana = CurrentMana.Value;
        OnDisplayedManaChanged?.Invoke();
    }

    public void ApplyClassPresetStats()
    {
        MaxHealth = UnitData.MaxHealth;
        CurrentHealth.Value = MaxHealth;
        DisplayedHealth = MaxHealth;

        MaxMana = UnitData.MaxMana;

        UnitStats.Strength = UnitData.Strength;
        UnitStats.Dexterity = UnitData.Dexterity;
        UnitStats.Constitution = UnitData.Constitution;
        UnitStats.Intelligence = UnitData.Intelligence;
        UnitStats.Faith = UnitData.Faith;
        UnitStats.Charisma = UnitData.Charisma;
        UnitStats.Luck = UnitData.Luck;
    }

    public void RegenerateResources()
    {
        //TODO: determine how many resources to restore
        ServerUpdateMana(1);
    }

    private IEnumerator ClientStartTurn(UnitController controller)
    {
        if (controller != this)
            yield break;

        yield return statusEffectController.ClientProcStatusEffects(ActivationTime.StartOfTurn);

        foreach (RuntimeAbilityInstance abilityInstance in RuntimeAbilityInstances)
        {
            abilityInstance.ProgressCooldown();
            yield return null;
        }

        TurnManager.OnClientActionPhaseStarted?.Invoke(controller);
        yield return null;
    }

    public void ServerTurnInitialization(UnitController controller)
    {
        if (controller != this)
            return;

        statusEffectController.ServerProcStatusEffects(ActivationTime.StartOfTurn);
        //Beginning of turn status effects modify health values
    }

    public void ClientTurnInitialization(UnitController controller)
    {
        if (controller != this)
            return;

        StartCoroutine(ClientStartTurn(controller));
        //Beginning of turn status effects display
    }

    public void ServerBeginActionPhase(UnitController controller)
    {
        if (controller != this)
            return;

        if (!ServerIsAlive.Value)
            return;

        RegenerateResources();

        if (enemyController == null)
            return;

        enemyController.PickAction();
        //Action is decided and computed
    }

    public void ClientBeginActionPhase(UnitController controller)
    {
        if (controller != this)
            return;

        ClientUpdateMana(1);
        ActionPhaseStarted = true;
        TurnManager.OnRefreshUI?.Invoke(controller);
        //Enable UI
    }

    public void ServerEndTurn(UnitController controller)
    {
        if (controller != this)
            return;

        statusEffectController.ServerProcStatusEffects(ActivationTime.EndOfTurn);
        //End of turn status effects modify health values
    }

    public void ClientEndTurn(UnitController controller)
    {
        if (controller != this)
            return;

        ActionPhaseStarted = false;
        statusEffectController.ClientProcStatusEffects(ActivationTime.EndOfTurn);
        //End of turn status effects display
    }

    public void ServerTakeDamage(HitResult hitResult, bool syncDisplayedHealth = false)
    {
        if (!IsServer) return;

        if (!ServerIsAlive.Value) return;

        CurrentHealth.Value = (int)Mathf.Clamp(CurrentHealth.Value - hitResult.Damage, 0, MaxHealth);

        if (syncDisplayedHealth)
        {
            DisplayedHealth = CurrentHealth.Value;
            OnDisplayedHealthChanged?.Invoke();
        }

        if (CurrentHealth.Value <= 0)
            ServerDie();
    }

    public void ClientTakeDamage(HitResult hitResult)
    {
        DisplayedHealth = (int)Mathf.Clamp(DisplayedHealth - hitResult.Damage, 0, MaxHealth);
        DamageNumberManager.instance.SpawnDamageNumberAtPosition(hitResult, transform.position);
        OnDisplayedHealthChanged?.Invoke();
        if (DisplayedHealth <= 0)
        {
            ClientDie();
        }
    }

    public void ServerHeal(int amount, bool syncDisplayedHealth = false)
    {
        if (!IsServer) return;

        if (!ServerIsAlive.Value) return;

        CurrentHealth.Value = (int)Mathf.Clamp(CurrentHealth.Value + amount, 0, MaxHealth);

        if (syncDisplayedHealth)
        {
            DisplayedHealth = CurrentHealth.Value;
            OnDisplayedHealthChanged?.Invoke();
        }
    }

    public void ServerUpdateMana(int amount, bool syncDisplayMana = false)
    {
        if (!ServerIsAlive.Value) return;

        Debug.Log($"Increasing CurrentMana.Value for {gameObject.name} from {CurrentMana.Value} to {CurrentMana.Value + amount}");
        CurrentMana.Value = Mathf.Clamp(CurrentMana.Value + amount, 0, MaxMana);

        if (syncDisplayMana)
            ClientUpdateMana(amount);
    }

    public void ClientUpdateMana(int amount)
    {
        DisplayedMana = Mathf.Clamp(DisplayedMana += amount, 0, MaxMana);
        OnDisplayedManaChanged?.Invoke();
    }

    private void ServerDie()
    {
        if (!ServerIsAlive.Value)
            return;

        OnServerDie?.Invoke();
        ServerIsAlive.Value = false;
        if (TurnManager.instance.ServerCurrentTurnUnitController == this)
            TurnManager.instance.ServerMoveToNextTurn();
    }

    private void ClientDie()
    {
        if (!ClientIsAlive)
            return;

        TurnManager.instance.RemoveTurnEntryUI(this);
        OnClientDie?.Invoke();
        gameObject.SetActive(false);
        ClientIsAlive = false;
        if (TurnManager.instance.ClientCurrentTurnUnitController == this)
            TurnManager.instance.RequestClientTurnAdvance();
    }


    public IEnumerator PlayAbilitySequence(BaseAbility ability, AbilityResult abilityResult)
    {
        NetworkTransform networkTransform = GetComponent<NetworkTransform>();

        if (networkTransform != null)
            networkTransform.enabled = false;

        AbilityNamePresentationManager.instance.DisplayName(ability);

        SyncManaValues(this);

        yield return new WaitForSeconds(2f);

        yield return MoveToTargetIfNeeded(ability.MovesToTarget, NetworkUtilities.GetUnitControllerById(abilityResult.TargetId));

        yield return new WaitForSeconds(0.4f);

        for (int p = 0; p < abilityResult.AbilityEffectResults.Length; p++)
        {
            for (int o = 0; o < abilityResult.AbilityEffectResults[p].TargetResults.Length; o++)
            {
                UnitController target = NetworkUtilities.GetUnitControllerById(abilityResult.AbilityEffectResults[p].TargetResults[o].TargetId);
                for (int i = 0; i < abilityResult.AbilityEffectResults[p].TargetResults[o].Hits.Length; i++)
                {
                    target.ClientTakeDamage(abilityResult.AbilityEffectResults[p].TargetResults[o].Hits[i]);
                    yield return new WaitForSeconds(ability.abilityEffects[p].DurationBetweenHits);
                }
            }
        }

        yield return new WaitForSeconds(0.4f);

        yield return MoveToOriginalPositionIfNeeded(ability.MovesToTarget);

        if (networkTransform != null)
            networkTransform.enabled = true;

        TurnManager.instance.RequestClientTurnAdvance();

        yield return null;
    }

    public int GetAbilityIndex(RuntimeAbilityInstance ability)
    {
        return RuntimeAbilityInstances.IndexOf(ability);
    }

    public int GetAbilityIndex(BaseAbility ability)
    {
        return RuntimeAbilityInstances.IndexOf(RuntimeAbilityInstances.Where(t => t.Ability == ability).First());
    }

    private IEnumerator MoveToTargetIfNeeded(bool movesToTarget, UnitController selectedTarget)
    {
        if (!movesToTarget)
            yield break;

        startPos = transform.position;
        Vector3 endPos = selectedTarget.transform.position + selectedTarget.transform.forward * 2f;
        yield return MoveTo(endPos, 0.4f);
    }

    private IEnumerator MoveToOriginalPositionIfNeeded(bool movesToTarget)
    {
        if (!movesToTarget)
            yield break;

        Vector3 endPos = startPos;
        yield return MoveTo(endPos, 0.4f);
    }

    private IEnumerator MoveTo(Vector3 position, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, position, elapsedTime / duration);
            yield return null;
        }
        yield break;
    }

    public void EnableHighlight()
    {
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", highlightEmission);
    }

    public void DisableHighlight()
    {
        mat.DisableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", originalEmission);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.instance.AssignContextMenu(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EnableHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DisableHighlight();
    }

    public void RecalculateAllStats()
    {
        CachedStatsDirty = false;

        var stats = new Dictionary<StatType, float>();

        // BASE ATTRIBUTES
        stats[StatType.STR] = UnitStats.Strength;
        stats[StatType.DEX] = UnitStats.Dexterity;
        stats[StatType.CON] = UnitStats.Constitution;
        stats[StatType.INT] = UnitStats.Intelligence;
        stats[StatType.FTH] = UnitStats.Faith;
        stats[StatType.CHA] = UnitStats.Charisma;
        stats[StatType.LCK] = UnitStats.Luck;

        // INITIATIVE
        stats[StatType.InitiativeMin] =
            1f + (stats[StatType.DEX] * 0.2f);

        stats[StatType.InitiativeMax] =
            6f + (stats[StatType.DEX] * 0.3f);

        // CRIT
        stats[StatType.CritChance] =
            10f +
            (0.5f * stats[StatType.LCK]) +
            (0.2f * stats[StatType.DEX]);

        stats[StatType.CritDamage] =
            50f +
            stats[StatType.LCK] +
            (0.5f * stats[StatType.DEX]);

        // BLOCK
        stats[StatType.BlockChance] =
            10f +
            (0.5f * stats[StatType.CON]) +
            (0.2f * stats[StatType.STR]);

        stats[StatType.BlockDamageReduction] =
            50f +
            (0.2f * stats[StatType.CON]);

        // DODGE
        stats[StatType.DodgeChance] =
            10f +
            (0.5f * stats[StatType.LCK]) +
            (0.2f * stats[StatType.DEX]);

        // AGGRO
        stats[StatType.Aggro] =
            100f +
            stats[StatType.CON] -
            stats[StatType.CHA];

        // ENERGY
        stats[StatType.EnergyGain] =
            0.5f * stats[StatType.INT];

        // LOOT
        stats[StatType.LuckyDrop] =
            0.5f * stats[StatType.LCK];

        // HEALING
        stats[StatType.OutgoingHealing] =
            100f + (0.5f * stats[StatType.FTH]);

        stats[StatType.IncomingHealing] =
            100f + (0.5f * stats[StatType.FTH]);

        // APPLY MODIFIERS
        ApplyModifiers(stats);

        CachedStats = stats;
    }

    private void ApplyModifiers(Dictionary<StatType, float> stats)
    {
        foreach (var mod in StatModifiers)
        {
            if (!stats.ContainsKey(mod.stat))
                continue;

            stats[mod.stat] += mod.value;
        }
    }

    public float GetStatType(StatType stat)
    {
        if (CachedStatsDirty)
            RecalculateAllStats();

        return CachedStats.TryGetValue(stat, out var value) ? value : 0f;
    }
}

[Serializable]
public class UnitAttributes
{
    public int Strength;
    public int Dexterity;
    public int Constitution;
    public int Intelligence;
    public int Faith;
    public int Charisma;
    public int Luck;
}

[Serializable]
public class CombatStats
{
    public float InitiativeMin;
    public float InitiativeMax;
    public float CritChance;
    public float CritDamage;
    public float BlockChance;
    public float BlockDamageReduction;
    public float DodgeChance;
    public float Aggro;
    public float Lifesteal;
    public float EnergyGain;
    public float LuckyDrop;
    public float IncomingHealing;
    public float OutgoingHealing;
}
