using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class EnemyController : NetworkBehaviour
{
    public UnitController unitController;
    public int ExpValue = 10;
    public List<ItemSO> AvailableDrops;

    public int baseStrength;

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        unitController.ServerIsAlive.OnValueChanged += OnDeathEvents;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        unitController.ServerIsAlive.OnValueChanged -= OnDeathEvents;
    }

    private void OnDeathEvents(bool oldValue, bool newValue)
    {
        if (newValue == false)
        {
            BattleManager.instance.DistributeExp(ExpValue);
            BattleManager.instance.DistributeItemsToPlayer(AvailableDrops[Random.Range(0, AvailableDrops.Count)]);
        }
    }

    public void PickAction()
    {
        if (!IsServer)
            return;

        List<RuntimeAbilityInstance> validAbilities = ReturnListOfValidAbilities();    //This generates a list of valid abilities

        if (validAbilities.Count == 0)
        {
            Debug.LogWarning("Could not find any valid abilities");
            return;
        }

        RuntimeAbilityInstance chosenAbility = ChooseAbility(validAbilities);  //This chooses a random valid ability

        List<UnitController> validTargets = GetValidTargets(chosenAbility); //This returns a list of all valid targets

        if (validTargets.Count == 0)
        {
            Debug.LogWarning("Could not find any valid targets");
            return;
        }

        UnitController primaryTarget = validTargets[Random.Range(0, validTargets.Count)]; //This picks a random valid target

        ulong targetId = primaryTarget.NetworkObjectId;

        CombatManager.instance.RequestCombatActionServerRpc(unitController.NetworkObjectId, unitController.GetAbilityIndex(chosenAbility), targetId);  //This requests a combat action on the targets
    }

    private List<RuntimeAbilityInstance> ReturnListOfValidAbilities()
    {
        List<RuntimeAbilityInstance> validAbilities = new();

        foreach (RuntimeAbilityInstance runtimeAbility in unitController.RuntimeAbilityInstances)
        {
            if (!runtimeAbility.CanUse(unitController))
                continue;

            List<UnitController> validTargets = GetValidTargets(runtimeAbility);
            if (validTargets.Count == 0)
                continue;

            if (!AbilityIntentConditionMet(runtimeAbility, validTargets))
                continue;

            validAbilities.Add(runtimeAbility);
        }

        return validAbilities;
    }

    public bool AbilityIntentConditionMet(RuntimeAbilityInstance ability, List<UnitController> validTargets)
    {
        foreach (AIIntent intent in ability.Ability.AIIntents)
        {
            switch (intent)
            {
                case AIIntent.Damage:
                    break;
                case AIIntent.Heal:
                    bool hasInjuredTarget = validTargets.Any(t => t.CurrentHealth.Value < t.MaxHealth);
                    if (!hasInjuredTarget)
                        return false;
                    break;
                case AIIntent.SelfHeal:
                    if (unitController.CurrentHealth.Value == unitController.MaxHealth)
                        return false;
                    break;
                case AIIntent.AOEDamage:
                    if (validTargets.Count <= 1)
                        return false;
                    break;
                default:
                    Debug.LogWarning($"Unhandled AIIntent: {intent}");
                    break;
            }
        }
        return true;
    }

    private List<UnitController> GetValidTargets(RuntimeAbilityInstance ability)
    {
        List<UnitController> targets = new();

        if (ability.Ability.TargetType == TargetType.Self)
        {
            targets.Add(unitController);
            return targets;
        }

        targets =
        ability.Ability.TeamTargeting == Team.Enemy
        ? BattleManager.instance.FriendlyUnits
        : BattleManager.instance.EnemyUnits;

        targets = targets
        .Where(t => t.ServerIsAlive.Value)
        .ToList();

        if (ability.Ability.AIIntents.Contains(AIIntent.Heal))
            targets = targets.Where(t => t.CurrentHealth.Value < t.MaxHealth).ToList();

        return targets;
    }

    private RuntimeAbilityInstance ChooseAbility(List<RuntimeAbilityInstance> validAbilities)
    {
        Dictionary<RuntimeAbilityInstance, float> scores = new();

        foreach (RuntimeAbilityInstance ability in validAbilities)
        {
            float score = ScoreAbility(ability);

            scores.Add(ability, score);
        }

        return ChooseWeightedAbility(scores);
    }

    private float ScoreAbility(RuntimeAbilityInstance ability)
    {
        float score = 1f;

        List<UnitController> validTargets = GetValidTargets(ability);

        foreach (AIIntent intent in ability.Ability.AIIntents)
        {
            switch (intent)
            {
                case AIIntent.Damage:
                    score += 4f;
                    break;

                case AIIntent.Heal:
                    float missingHealth = validTargets
                        .Max(t => t.MaxHealth - t.CurrentHealth.Value);
                    score += missingHealth;
                    break;

                case AIIntent.SelfHeal:
                    float selfMissing = unitController.MaxHealth - unitController.CurrentHealth.Value;
                    score += selfMissing;
                    break;

                case AIIntent.AOEDamage:
                    score += validTargets.Count * 5f;
                    break;
            }
        }

        float manaPressure = unitController.CurrentMana.Value;
        if (ability.Ability.ManaCost > 0)
        {
            score += manaPressure * 3f;
        }

        score += ability.Ability.ManaCost * 0.5f;
        return Mathf.Max(1f, score);
    }

    private RuntimeAbilityInstance ChooseWeightedAbility(Dictionary<RuntimeAbilityInstance, float> scores)
    {
        float totalWeight = scores.Values.Sum();

        float roll = Random.Range(0f, totalWeight);

        foreach (var pair in scores)
        {
            roll -= pair.Value;

            if (roll <= 0f)
                return pair.Key;
        }

        return scores.Keys.First();
    }
}
