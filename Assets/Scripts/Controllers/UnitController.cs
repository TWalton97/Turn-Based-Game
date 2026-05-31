using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;
using System.Linq;
using Unity.Netcode.Components;

public class UnitController : NetworkBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public string UnitName;

    public UnitAttributes UnitStats;

    public ClassStatPresetSO UnitData;
    public float MaxHealth = 30;
    public NetworkVariable<float> CurrentHealth;
    public float DisplayedHealth;

    public int Level;

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

    public bool ServerIsStunned;
    public bool ClientIsStunned;

    public EnemyController enemyController { get; private set; }
    public StatusEffectController statusEffectController { get; private set; }
    private Vector3 startPos;

    public List<StatModifier> StatModifiers;
    public Dictionary<StatType, float> CachedStats;
    public List<StateModifier> StateModifiers;
    public bool CachedStatsDirty = true;

    private int nextTurnManaRegen;

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

        UnlockAbilities();
        ApplyClassPresetStats();
        RecalculateAllStats();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        BattleManager.instance.UnregisterUnit(this);
    }

    public void UnlockAbilities()
    {
        foreach (AbilityUnlock abilityUnlock in UnitData.AbilityUnlocks)
        {
            if (abilityUnlock.LevelToUnlock <= Level && !BaseAbilities.Contains(abilityUnlock.AbilityToUnlock))
            {
                BaseAbilities.Add(abilityUnlock.AbilityToUnlock);
            }
        }
        InitializeAbilityRuntimeInstances();
    }

    public void InitializeAbilityRuntimeInstances()
    {
        HashSet<BaseAbility> existing = new HashSet<BaseAbility>(RuntimeAbilityInstances.Select(r => r.Ability));

        foreach (BaseAbility ability in BaseAbilities)
        {
            if (!existing.Contains(ability))
            {
                RuntimeAbilityInstance runtimeAbilityInstance = new();
                runtimeAbilityInstance.Ability = ability;
                RuntimeAbilityInstances.Add(runtimeAbilityInstance);
            }
        }
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

    public void ServerRegenerateResources()
    {
        nextTurnManaRegen = CalculateManaRegen();
        ServerUpdateMana(nextTurnManaRegen);
    }

    public void ClientRegenerateResources()
    {
        ClientUpdateMana(nextTurnManaRegen);
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
        statusEffectController.ServerProcStatusEffects(ActivationTime.OnApplication);
        statusEffectController.ServerReduceRemainingTurnTimer();
        //Beginning of turn status effects modify health values
    }

    public void ClientTurnInitialization(UnitController controller)
    {
        if (controller != this)
            return;

        StartCoroutine(ClientStartTurn(controller));
        statusEffectController.ClientProcStatusEffects(ActivationTime.OnApplication);
        statusEffectController.ClientReduceRemainingTurnTimer();
        //Beginning of turn status effects display
    }

    public void ServerBeginActionPhase(UnitController controller)
    {
        if (controller != this)
            return;

        if (!ServerIsAlive.Value)
            return;

        ServerRegenerateResources();

        if (StateModifiers.Exists(s => s.stateTag == UnitStateTags.Stunned))
        {
            TurnManager.instance.ServerMoveToNextTurn();
            return;
        }

        if (enemyController == null)
            return;

        enemyController.PickAction();
        //Action is decided and computed
    }

    public void ClientBeginActionPhase(UnitController controller)
    {
        if (controller != this)
            return;

        ClientRegenerateResources();

        if (StateModifiers.Exists(s => s.stateTag == UnitStateTags.Stunned))
        {
            DamageNumberManager.instance.SpawnStunnedTextAtPosition(controller.transform.position);
            TurnManager.instance.RequestClientTurnAdvance();
            return;
        }

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

        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value - hitResult.Damage, 0, MaxHealth);

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
        DisplayedHealth = Mathf.Clamp(DisplayedHealth - hitResult.Damage, 0, MaxHealth);
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

    public void ServerUpdateMana(int amount)
    {
        if (!ServerIsAlive.Value) return;

        CurrentMana.Value = Mathf.Clamp(CurrentMana.Value + amount, 0, MaxMana);
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

        ClientUpdateMana(-ability.ManaCost);

        yield return new WaitForSeconds(ability.AbilityAnimationDelay);

        yield return MoveToTargetIfNeeded(ability.MovesToTarget, NetworkUtilities.GetUnitControllerById(abilityResult.TargetId));

        yield return new WaitForSeconds(0.4f);

        for (int p = 0; p < abilityResult.AbilityEffectResults.Length; p++)
        {
            if (abilityResult.AbilityEffectResults[p].ApplyStatusEffect)
            {
                List<StatusEffectInstance> instancesWithId = statusEffectController.ActiveStatusEffects.Where(t => t.statusEffectId == abilityResult.AbilityEffectResults[p].StatusEffectId).ToList();
                foreach (StatusEffectInstance instance in instancesWithId)
                {
                    instance.StatusEffect.ClientOnApplication(this, instance);
                }
            }

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

        // GENERIC DAMAGE MODIFIERS
        stats[StatType.IncomingDamage] =
            100f;

        stats[StatType.OutgoingDamage] =
            100f;

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

    public int CalculateManaRegen()
    {
        float energyGain = GetStatType(StatType.EnergyGain);

        int manaRegen = 1;

        // Guaranteed extra mana
        manaRegen += Mathf.FloorToInt(energyGain / 100f);

        // Chance for one additional mana
        float remainder = energyGain % 100f;

        if (UnityEngine.Random.Range(0f, 100f) < remainder)
        {
            manaRegen++;
        }

        return manaRegen;
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

public enum UnitStateTags
{
    Stunned,
    Silenced,
}
