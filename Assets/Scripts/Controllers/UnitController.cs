using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;
using System.Linq;
using Unity.Netcode.Components;
using Unity.Collections;

public class UnitController : NetworkBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public string UnitName;

    public NetworkVariable<int> Strength;
    public NetworkVariable<int> Dexterity;
    public NetworkVariable<int> Intelligence;
    public NetworkVariable<int> Constitution;

    public ClassStatPresetSO UnitData;
    public float MaxHealth;
    public NetworkVariable<float> CurrentHealth;
    public float DisplayedHealth;
    private float missingHealth;

    public NetworkVariable<int> Level;

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
    private MaterialPropertyBlock mpb;
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

    public NetworkList<StatModifier> StatModifiers;
    public Dictionary<StatType, float> CachedStats;
    public List<StateModifier> StateModifiers;
    public bool CachedStatsDirty = true;

    private Animator animator;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        mpb = new MaterialPropertyBlock();
        originalEmission = meshRenderer.material.GetColor("_EmissionColor");

        enemyController = GetComponent<EnemyController>();
        statusEffectController = GetComponent<StatusEffectController>();

        TurnManager.OnServerTurnStarted += ServerTurnInitialization;
        TurnManager.OnServerActionPhaseStarted += ServerBeginActionPhase;
        TurnManager.OnServerTurnEnded += ServerEndTurn;

        TurnManager.OnClientTurnStarted += ClientTurnInitialization;
        TurnManager.OnClientActionPhaseStarted += ClientBeginActionPhase;
        TurnManager.OnClientTurnEnded += ClientEndTurn;

        animator = GetComponentInChildren<Animator>();

        StatModifiers = new();
    }

    public override void OnNetworkSpawn()
    {
        ServerIsAlive.Value = true;
        ClientIsAlive = true;

        BattleManager.instance.RegisterUnit(this);

        UnlockStartingAbilities();
        StartCoroutine(WaitUntilMaxHealthIsSet());
    }

    private IEnumerator WaitUntilMaxHealthIsSet()
    {
        yield return new WaitUntil(() => CurrentHealth.Value > 0);
        MaxHealth = CurrentHealth.Value;
        DisplayedHealth = CurrentHealth.Value;
        yield return null;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        BattleManager.instance.UnregisterUnit(this);
    }

    public void UnlockStartingAbilities()
    {
        List<AbilityUnlock> abilityUnlocks = UnitData.AbilityUnlocks.Where(t => t.LevelToUnlock <= Level.Value).ToList();

        foreach (AbilityUnlock abilityUnlock in abilityUnlocks)
        {
            if (abilityUnlock.AbilityUnlockType == AbilityUnlockType.AutoGrant && abilityUnlock.AbilityToUnlock.Count > 0)
                UnlockAbility(abilityUnlock.AbilityToUnlock[0]);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UnlockAbilityServerRpc(FixedString64Bytes abilityName)
    {
        BaseAbility ability = AbilityDatabase.GetAbilityByName(abilityName);

        if (BaseAbilities.Contains(ability))
            return;

        BaseAbilities.Add(ability);
        InitializeAbilityRuntimeInstances();
        UnlockAbilityClientRpc(abilityName);
    }

    [ClientRpc]
    public void UnlockAbilityClientRpc(FixedString64Bytes abilityName)
    {
        BaseAbility ability = AbilityDatabase.GetAbilityByName(abilityName);

        if (BaseAbilities.Contains(ability))
            return;

        BaseAbilities.Add(ability);
        InitializeAbilityRuntimeInstances();
    }

    private void UnlockAbility(BaseAbility ability)
    {
        if (BaseAbilities.Contains(ability))
            return;

        BaseAbilities.Add(ability);
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
        if (IsServer)
        {
            float levelMultiplier = Mathf.Max(0, Level.Value - 1);

            float total = UnitData.Strength + UnitData.Dexterity + UnitData.Intelligence;

            float strengthWeight = UnitData.Strength / total;
            float dexterityWeight = UnitData.Dexterity / total;
            float intelligenceWeight = UnitData.Intelligence / total;

            Strength.Value = UnitData.Strength + Mathf.RoundToInt(levelMultiplier * (0.75f + (strengthWeight * 0.75f)));
            Dexterity.Value = UnitData.Dexterity + Mathf.RoundToInt(levelMultiplier * (0.75f + (dexterityWeight * 0.75f)));
            Intelligence.Value = UnitData.Intelligence + Mathf.RoundToInt(levelMultiplier * (0.75f + (intelligenceWeight * 0.75f)));
            Constitution.Value = UnitData.Constitution;
        }

        RecalculateAllStats();

        if (IsServer)
        {
            CurrentHealth.Value = MaxHealth;
        }

        MaxMana = UnitData.MaxMana;
    }


    public void CombatEndReset()
    {
        if (IsServer)
            CurrentMana.Value = 0;

        DisplayedMana = CurrentMana.Value;

        foreach (RuntimeAbilityInstance abilityInstance in RuntimeAbilityInstances)
        {
            abilityInstance.RemainingCooldownTurns = 0;
        }

        statusEffectController.ClearAllStatusEffects();
    }

    public void ServerRegenerateResources()
    {
        ServerUpdateMana(CalculateManaRegen());
    }

    public void ClientRegenerateResources()
    {
        ClientUpdateMana(CalculateManaRegen());
    }

    public void ServerTurnInitialization(UnitController controller)
    {
        if (controller != this)
            return;

        statusEffectController.ServerOnTurnStarted();
    }

    public void ClientTurnInitialization(UnitController controller)
    {
        if (controller != this)
            return;

        StartCoroutine(ClientStartTurn(controller));
    }

    private IEnumerator ClientStartTurn(UnitController controller)
    {
        if (controller != this)
            yield break;

        yield return statusEffectController.ClientOnTurnStarted();

        foreach (RuntimeAbilityInstance abilityInstance in RuntimeAbilityInstances)
        {
            abilityInstance.ProgressCooldown();
            yield return null;
        }

        TurnManager.OnClientActionPhaseStarted?.Invoke(controller);
        yield return null;
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
    }

    public void ServerEndTurn(UnitController controller)
    {
        if (controller != this)
            return;

        statusEffectController.ServerOnTurnEnded();
        //End of turn status effects modify health values
    }

    public void ClientEndTurn(UnitController controller)
    {
        if (controller != this)
            return;

        ActionPhaseStarted = false;
        statusEffectController.ClientOnTurnEnded();
        //End of turn status effects display
    }

    public void ServerTakeDamage(HitResult hitResult, bool syncDisplayedHealth = false)
    {
        if (!IsServer) return;

        if (!ServerIsAlive.Value) return;

        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value - hitResult.Damage, 0, MaxHealth);

        if (hitResult.DamageSource != DamageSource.StatusEffect && !hitResult.Dodged)
            statusEffectController.ServerOnHit();

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

        animator.SetTrigger("TakeDamage");

        if (hitResult.DamageSource != DamageSource.StatusEffect && !hitResult.Dodged)
            statusEffectController.ClientOnHit();

        OnDisplayedHealthChanged?.Invoke();
        if (DisplayedHealth <= 0)
        {
            ClientDie();
        }
    }

    public void ServerHeal(float amount, bool syncDisplayedHealth = false)
    {
        if (!IsServer) return;

        if (!ServerIsAlive.Value) return;

        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value + amount, 0, MaxHealth);

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

        animator.SetBool("IsDead", true);

        TurnManager.instance.RemoveTurnEntryUI(this);
        OnClientDie?.Invoke();
        //gameObject.SetActive(false);
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

        PlayCorrectAttackAnimation(ability);
        for (int p = 0; p < abilityResult.AbilityEffectResults.Length; p++)
        {
            for (int o = 0; o < abilityResult.AbilityEffectResults[p].TargetResults.Length; o++)
            {
                UnitController target = NetworkUtilities.GetUnitControllerById(abilityResult.AbilityEffectResults[p].TargetResults[o].TargetId);
                for (int i = 0; i < abilityResult.AbilityEffectResults[p].TargetResults[o].Hits.Length; i++)
                {
                    statusEffectController.ClientOnAttack();
                    target.ClientTakeDamage(abilityResult.AbilityEffectResults[p].TargetResults[o].Hits[i]);
                    yield return new WaitForSeconds(ability.abilityEffects[p].DurationBetweenHits);
                }

                if (abilityResult.AbilityEffectResults[p].ApplyStatusEffect)
                {
                    StatusEffect status = StatusDatabase.GetStatusByName(abilityResult.AbilityEffectResults[p].StatusEffectName);
                    target.statusEffectController.ClientApplyStatusEffect(status, abilityResult.AttackerId, abilityResult.AbilityEffectResults[p].StatusEffectId, abilityResult.AbilityEffectResults[p].StatusEffectResolvedPower);
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

    private void PlayCorrectAttackAnimation(BaseAbility ability)
    {
        switch (ability.AnimationType)
        {
            case AbilityAnimationType.LightAttack:
                animator.SetTrigger("MeleeLight");
                break;
            case AbilityAnimationType.HeavyAttack:
                animator.SetTrigger("MeleeHeavy");
                break;
            case AbilityAnimationType.Spell:
                animator.SetTrigger("Spell");
                break;
            case AbilityAnimationType.Heal:
                animator.SetTrigger("Heal");
                break;
        }
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
        yield return MoveTo(endPos, 0.3f);
    }

    private IEnumerator MoveToOriginalPositionIfNeeded(bool movesToTarget)
    {
        if (!movesToTarget)
            yield break;

        Vector3 endPos = startPos;
        yield return MoveTo(endPos, 0.3f);
    }

    private IEnumerator MoveTo(Vector3 position, float duration)
    {
        animator.SetBool("Walking", true);
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, position, elapsedTime / duration);
            yield return null;
        }
        animator.SetBool("Walking", false);
        yield break;
    }

    public void EnableHighlight()
    {
        meshRenderer.material.EnableKeyword("_EMISSION");

        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_EmissionColor", highlightEmission);
        meshRenderer.SetPropertyBlock(mpb);
    }

    public void DisableHighlight()
    {
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_EmissionColor", originalEmission);
        meshRenderer.SetPropertyBlock(mpb);

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

    public void RecalculateAllStats()
    {
        var stats = new Dictionary<StatType, float>();

        MaxHealth = Constitution.Value * 33;

        // BASE ATTRIBUTES
        stats[StatType.STR] = Strength.Value;
        stats[StatType.DEX] = Dexterity.Value;
        stats[StatType.CON] = Constitution.Value;
        stats[StatType.INT] = Intelligence.Value;

        // APPLY MODIFIERS
        ApplyAttributeModifiers(stats);

        // INITIATIVE
        stats[StatType.InitiativeMin] =
            1f + (stats[StatType.DEX] * 0.2f);

        stats[StatType.InitiativeMax] =
            6f + (stats[StatType.DEX] * 0.3f);

        // CRIT
        stats[StatType.CritChance] =
            10f + (0.2f * stats[StatType.INT]);

        stats[StatType.CritDamage] =
            50f + (0.5f * stats[StatType.INT]);

        // BLOCK
        stats[StatType.BlockChance] =
            5f + (0.5f * stats[StatType.CON]);

        stats[StatType.BlockDamageReduction] =
            50f + (0.2f * stats[StatType.CON]);

        // DODGE
        stats[StatType.DodgeChance] =
            5f + (0.2f * stats[StatType.DEX]);

        // AGGRO
        stats[StatType.Aggro] =
            100f;

        // ENERGY
        stats[StatType.EnergyGain] =
            0.5f * stats[StatType.INT];

        // LOOT
        stats[StatType.LuckyDrop] =
            0.5f;

        // HEALING
        stats[StatType.OutgoingHealing] =
            100f;

        stats[StatType.IncomingHealing] =
            100f;

        // GENERIC DAMAGE MODIFIERS
        stats[StatType.IncomingDamage] =
            100f;

        stats[StatType.OutgoingDamage] =
            100f;

        // APPLY EXTRA MODIFIERS
        ApplyExtraModifiers(stats);

        CachedStats = stats;
    }

    private void ApplyExtraModifiers(Dictionary<StatType, float> stats)
    {
        foreach (var mod in StatModifiers)
        {
            if (AttributeStats.Contains(mod.stat))
                continue;

            if (!stats.ContainsKey(mod.stat))
                continue;

            stats[mod.stat] += mod.value;
        }
    }

    private void ApplyAttributeModifiers(Dictionary<StatType, float> stats)
    {
        foreach (var mod in StatModifiers)
        {
            if (!AttributeStats.Contains(mod.stat))
                continue;

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

        float totalMana = 1f + (energyGain / 100f);

        int guaranteedMana = Mathf.FloorToInt(totalMana);
        float chanceForExtra = (totalMana - guaranteedMana) * 100f;

        if (UnityEngine.Random.Range(0f, 100f) < chanceForExtra)
        {
            guaranteedMana++;
        }

        return guaranteedMana;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestConfirmSkillpointChangesServerRpc(ulong unitId, StatAllocation statAllocation, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        UnitController controller = NetworkUtilities.GetUnitControllerById(unitId);
        NetworkObject unitObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[unitId];
        PlayerDataController playerDataController = controller.GetComponent<PlayerDataController>();

        if (unitObj.OwnerClientId != clientId)
        {
            Debug.LogWarning($"Client attempting to send a request for a unit they do not own");
            return;
        }

        int totalPoints = 0;
        for (int i = 0; i < statAllocation.statChanges.Length; i++)
        {
            totalPoints += statAllocation.statChanges[i].amount;
        }

        if (totalPoints > playerDataController.AvailableStatPoints.Value)
        {
            Debug.LogWarning($"Unit with it {unitId} is trying to spend more skillpoints than they have");
            return;
        }

        playerDataController.AvailableStatPoints.Value -= totalPoints;


        foreach (var change in statAllocation.statChanges)
        {
            switch (change.statType)
            {
                case StatType.STR:
                    controller.Strength.Value += change.amount;
                    break;

                case StatType.DEX:
                    controller.Dexterity.Value += change.amount;
                    break;

                case StatType.CON:
                    missingHealth = MaxHealth - CurrentHealth.Value;
                    controller.Constitution.Value += change.amount;
                    break;

                case StatType.INT:
                    controller.Intelligence.Value += change.amount;
                    break;
            }
        }

        controller.CachedStatsDirty = true;
        controller.RecalculateAllStats();
        controller.CurrentHealth.Value = MaxHealth - missingHealth;
        UpdateDisplayedHealthClientRpc();
    }

    [ClientRpc]
    private void UpdateDisplayedHealthClientRpc()
    {
        DisplayedHealth = CurrentHealth.Value;
        OnDisplayedHealthChanged?.Invoke();
    }

    private readonly HashSet<StatType> AttributeStats = new()
    {
        StatType.STR,
        StatType.DEX,
        StatType.INT,
        StatType.CON,
    };
}


public enum UnitStateTags
{
    Stunned,
    Silenced,
    Taunted,
}
