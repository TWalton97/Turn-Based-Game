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

    public UnitStats UnitStats;
    public CombatStats CombatStats;

    public ClassStatPresetSO UnitData;
    public int MaxHealth = 30;
    public NetworkVariable<int> CurrentHealth;
    public int DisplayedHealth;

    public int MaxMana = 5;
    public NetworkVariable<int> CurrentMana;
    public int DisplayedMana;

    public NetworkVariable<bool> IsAlive;

    public Team UnitTeam;

    public List<BaseAbility> Abilities;

    private MeshRenderer meshRenderer;
    private Material mat;

    private Color originalEmission;

    [SerializeField] private Color highlightEmission = Color.white * 2f;

    public Action OnDisplayedManaChanged;
    public Action OnDisplayedHealthChanged;
    public Action OnDie;

    private List<UnitController> unitControllers = new();

    public EnemyController enemyController { get; private set; }
    private Vector3 startPos;

    public Dictionary<Attribute, Action<float>> statSetters;



    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        mat = meshRenderer.material;
        originalEmission = mat.GetColor("_EmissionColor");

        enemyController = GetComponent<EnemyController>();

        statSetters = new Dictionary<Attribute, Action<float>>
        {
            { Attribute.STR, v => UnitStats.Strength += (int)v },
            { Attribute.DEX, v => UnitStats.Dexterity += (int)v },
            { Attribute.CON, v => UnitStats.Constitution += (int)v },
            { Attribute.INT, v => UnitStats.Intelligence += (int)v },
            { Attribute.FTH, v => UnitStats.Faith += (int)v },
            { Attribute.CHA, v => UnitStats.Charisma += (int)v },
            { Attribute.LCK, v => UnitStats.Luck += (int)v },
        };
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        IsAlive.Value = true;

        BattleManager.instance.RegisterUnit(this);

        ApplyClassPresetStats();
        RecalculateCombatStats();

        CurrentHealth.OnValueChanged += OnHealthChanged;
        CurrentMana.OnValueChanged += OnManaChanged;

        TurnManager.OnRefreshUI += SyncHealthAndManaValues;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        BattleManager.instance.UnregisterUnit(this);
        TurnManager.OnRefreshUI -= SyncHealthAndManaValues;
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {

    }

    public void ApplyPendingHealthVisual()
    {

    }

    private void OnManaChanged(int oldValue, int newValue)
    {

    }

    public void SyncHealthAndManaValues(UnitController controller)
    {
        DisplayedHealth = CurrentHealth.Value;
        DisplayedMana = CurrentMana.Value;
        OnDisplayedHealthChanged?.Invoke();
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

    public void ProcessStatusEffects()
    {
        //TODO: status effects will go here
    }

    public void ServerBeginTurn()
    {
        if (enemyController == null)
            return;

        enemyController.PickAction();
    }

    public void ProcessEndTurnEffects()
    {
        //TODO: end of turn effects will go here
    }

    public void ServerTakeDamage(HitResult hitResult, bool syncDisplayedHealth = false)
    {
        if (!IsServer) return;

        if (!IsAlive.Value) return;

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

        if (!IsAlive.Value) return;

        CurrentHealth.Value = (int)Mathf.Clamp(CurrentHealth.Value + amount, 0, MaxHealth);

        if (syncDisplayedHealth)
        {
            DisplayedHealth = CurrentHealth.Value;
            OnDisplayedHealthChanged?.Invoke();
        }
    }


    public void ServerUpdateMana(int amount, bool syncDisplayedMana = false)
    {
        if (!IsAlive.Value) return;

        CurrentMana.Value = Mathf.Clamp(CurrentMana.Value + amount, 0, MaxMana);

        if (syncDisplayedMana)
            ClientUpdateMana();
    }

    public void ClientUpdateMana()
    {
        DisplayedMana = CurrentMana.Value;
        OnDisplayedManaChanged?.Invoke();
    }

    private void ServerDie()
    {
        TurnManager.instance.RemoveTurnEntryList(this);
        IsAlive.Value = false;
    }

    private void ClientDie()
    {
        TurnManager.instance.RemoveTurnEntryUI(this);
        OnDie?.Invoke();
        gameObject.SetActive(false);
    }

    public IEnumerator PlayAbilitySequence(BaseAbility ability, AbilityResult abilityResult)
    {
        NetworkTransform networkTransform = GetComponent<NetworkTransform>();

        if (networkTransform != null)
            networkTransform.enabled = false;

        yield return new WaitForSeconds(0.5f);

        yield return MoveToTargetIfNeeded(ability.MovesToTarget, NetworkUtilities.GetUnitControllerById(abilityResult.TargetResults[0].TargetId));

        yield return new WaitForSeconds(0.4f);

        for (int i = 0; i < ability.NumberOfHits; i++)
        {
            for (int p = 0; p < abilityResult.TargetResults.Length; p++)
            {
                UnitController target = NetworkUtilities.GetUnitControllerById(abilityResult.TargetResults[p].TargetId);
                target.ClientTakeDamage(abilityResult.TargetResults[p].Hits[i]);
            }
            yield return new WaitForSeconds(ability.DurationBetweenHits);
        }

        yield return new WaitForSeconds(0.4f);

        yield return MoveToOriginalPositionIfNeeded(ability.MovesToTarget);

        yield return new WaitForSeconds(0.4f);

        if (networkTransform != null)
            networkTransform.enabled = true;

        yield return null;
    }

    public int GetAbilityIndex(BaseAbility ability)
    {
        return Abilities.IndexOf(ability);
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

    public void RecalculateCombatStats()
    {
        CombatStats.InitiativeMin = 1 + (UnitStats.Dexterity * 0.2f);
        CombatStats.InitiativeMax = 6 + (UnitStats.Dexterity * 0.3f);

        CombatStats.CritChance = 10.0f + (0.5f * UnitStats.Luck) + (0.2f * UnitStats.Dexterity);
        CombatStats.CritDamage = 50.0f + UnitStats.Luck + (0.5f * UnitStats.Dexterity);

        CombatStats.BlockChance = 10.0f + (0.5f * UnitStats.Constitution) + (0.2f * UnitStats.Strength);
        CombatStats.BlockDamageReduction = 50.0f + (0.2f * UnitStats.Constitution);

        CombatStats.DodgeChance = 10.0f + (0.5f * UnitStats.Luck) + (0.2f * UnitStats.Dexterity);

        CombatStats.Aggro = 100.0f + UnitStats.Constitution - UnitStats.Charisma;

        CombatStats.EnergyGain = 0.5f * UnitStats.Intelligence;

        CombatStats.LuckyDrop = 0.5f * UnitStats.Luck;

        CombatStats.OutgoingHealing = 100.0f + 0.5f * UnitStats.Faith;
        CombatStats.IncomingHealing = 100.0f + 0.5f * UnitStats.Faith;
    }
}

[Serializable]
public class UnitStats
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
