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
    public bool IsActiveTurn = false;

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

        if (!IsServer)
        {
            CurrentHealth.OnValueChanged += OnHealthChanged;
            CurrentMana.OnValueChanged += OnManaChanged;

            IsAlive.OnValueChanged += OnIsDeadChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        BattleManager.instance.UnregisterUnit(this);
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {

    }

    public void ApplyPendingHealthVisual()
    {

    }

    private void OnManaChanged(int oldValue, int newValue)
    {
        OnDisplayedManaChanged?.Invoke();
    }

    private void OnIsDeadChanged(bool oldValue, bool newValue)
    {
        gameObject.SetActive(false);
        OnDie?.Invoke();
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
        UpdateMana(-1);
    }

    public void ProcessStatusEffects()
    {
        //TODO: status effects will go here
    }

    public void BeginActionPhase()
    {
        IsActiveTurn = true;

        //If there is an enemy controller, let it decide
        //Otherwise we want to display the UI
        if (enemyController == null)
            return;

        enemyController.PickAction();
    }

    public void BeginClientTurn()
    {
        if (!IsOwner)
            return;

        IsActiveTurn = true;
    }

    public void EndClientTurn()
    {
        if (!IsOwner)
            return;

        IsActiveTurn = false;
    }

    public void ProcessEndTurnEffects()
    {
        //TODO: end of turn effects will go here
    }

    public void TakeDamage(DamageResult damageResult, bool syncDisplayedHealth = false)
    {
        if (!IsServer) return;

        if (!IsAlive.Value) return;

        CurrentHealth.Value = (int)Mathf.Clamp(CurrentHealth.Value - damageResult.Damage, 0, MaxHealth);

        if (syncDisplayedHealth)
        {
            DisplayedHealth = (int)Mathf.Clamp(CurrentHealth.Value - damageResult.Damage, 0, MaxHealth);
            OnDisplayedHealthChanged?.Invoke();
        }

        if (CurrentHealth.Value <= 0)
            Die();
    }

    public void Heal(int amount, bool syncDisplayedHealth = false)
    {
        if (!IsServer) return;

        if (!IsAlive.Value) return;

        CurrentHealth.Value = (int)Mathf.Clamp(CurrentHealth.Value + amount, 0, MaxHealth);

        if (syncDisplayedHealth)
        {
            DisplayedMana = (int)Mathf.Clamp(CurrentHealth.Value + amount, 0, MaxHealth);
            OnDisplayedManaChanged?.Invoke();
        }
    }

    public void UpdateMana(int amount)
    {
        if (!IsAlive.Value) return;

        CurrentMana.Value = Mathf.Clamp(CurrentMana.Value - amount, 0, MaxMana);
        DisplayedMana = CurrentMana.Value;
        OnDisplayedManaChanged?.Invoke();
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " has died");
        OnDie?.Invoke();
        gameObject.SetActive(false);
        TurnManager.instance.RemoveUnitFromTurnEntries(this);
        IsAlive.Value = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestUseAbilityServerRpc(int abilityIndex, ulong targetId)
    {
        TryUseAbility(abilityIndex, targetId);
    }

    public void TryUseAbility(int abilityIndex, ulong targetId)
    {
        if (!IsServer)
            return;

        BaseAbility ability = Abilities[abilityIndex];
        UnitController target = GetTarget(targetId);
        ReturnTargetControllers(ability, target);

        ability.ConsumeCost(this);

        int numberOfHits = ability.NumberOfHits;

        bool[] dodged = new bool[numberOfHits];
        bool[] crit = new bool[numberOfHits];
        float[] damage = new float[numberOfHits];

        //We need to construct the damage output then pass this into the ExecuteAbility function
        for (int i = 0; i < ability.NumberOfHits; i++)
        {
            DamageResult damageResult = CombatResolver.CalculateDamage(this, ability, target);
            dodged[i] = damageResult.Dodged;
            crit[i] = damageResult.Crit;
            damage[i] = damageResult.Damage;
            foreach (UnitController controller in unitControllers)
            {
                controller.TakeDamage(damageResult);
            }
        }

        UseAbilityClientRpc(abilityIndex, targetId, dodged, crit, damage);
    }

    [ClientRpc]
    public void UseAbilityClientRpc(int abilityIndex, ulong targetId, bool[] dodged, bool[] crit, float[] damage)
    {
        StartCoroutine(PlayAbilitySequence(Abilities[abilityIndex], GetTarget(targetId), dodged, crit, damage));
    }

    private IEnumerator PlayAbilitySequence(BaseAbility ability, UnitController target, bool[] dodged, bool[] crit, float[] damage)
    {
        NetworkTransform networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform != null)
            networkTransform.enabled = false;

        IsActiveTurn = false;

        yield return new WaitForSeconds(0.5f);

        yield return MoveToTargetIfNeeded(ability.MovesToTarget, target);

        yield return new WaitForSeconds(0.4f);

        for (int i = 0; i < ability.NumberOfHits; i++)
        {
            DamageResult damageResult = new DamageResult();
            damageResult.Dodged = dodged[i];
            damageResult.Crit = crit[i];
            damageResult.Damage = damage[i];
            if (dodged[i])
            {
                DamageNumberManager.instance.SpawnDodgedTextAtPosition(target.transform.position);
            }
            else
            {
                DamageNumberManager.instance.SpawnDamageNumberAtPosition(damageResult, target.transform.position);
                target.DisplayedHealth -= (int)damageResult.Damage;
                target.OnDisplayedHealthChanged?.Invoke();
            }
            yield return new WaitForSeconds(ability.DurationBetweenHits);
        }

        yield return new WaitForSeconds(0.4f);

        yield return MoveToOriginalPositionIfNeeded(ability.MovesToTarget);

        yield return new WaitForSeconds(0.4f);

        if (networkTransform != null)
            networkTransform.enabled = true;
        RequestEndActionServerRpc();

        yield return null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestEndActionServerRpc()
    {
        if (!IsServer)
            return;

        TurnManager.instance.ResolveAction(this);
    }

    public void ApplyEffect(BaseAbility ability, UnitController selectedTarget)
    {
        if (!IsServer)
            return;

        foreach (UnitController controller in unitControllers)
        {
            DamageResult damageResult = CombatResolver.CalculateDamage(this, ability, selectedTarget);
            controller.TakeDamage(damageResult);
        }
    }

    public UnitController GetTarget(ulong targetId)
    {
        UnitController controller = BattleManager.instance.AllUnits.Find(t => t.NetworkObjectId == targetId);
        return controller;
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

    private List<UnitController> ReturnTargetControllers(BaseAbility ability, UnitController target)
    {
        unitControllers.Clear();

        if (ability.TargetType == TargetType.SingleUnit)
        {
            unitControllers.Add(target);
        }
        else
        {
            switch (ability.TeamTargeting)
            {
                case Team.Enemy:
                    foreach (UnitController controller in BattleManager.instance.EnemyUnits)
                    {
                        unitControllers.Add(controller);
                    }
                    break;

                case Team.Ally:
                    foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
                    {
                        unitControllers.Add(controller);
                    }
                    break;
            }
        }
        return unitControllers;
    }

    public void EnableHighlight()
    {
        meshRenderer.material.EnableKeyword("_EMISSION");
    }

    public void DisableHighlight()
    {
        meshRenderer.material.DisableKeyword("_EMISSION");
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
