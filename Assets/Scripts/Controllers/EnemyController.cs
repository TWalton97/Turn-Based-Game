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

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        unitController.IsAlive.OnValueChanged += OnDeathEvents;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        unitController.IsAlive.OnValueChanged -= OnDeathEvents;
    }

    private void OnDeathEvents(bool oldValue, bool newValue)
    {
        if (newValue == false)
        {
            BattleManager.instance.DistributeExpToPlayers(ExpValue);
            BattleManager.instance.DistributeItemsToPlayer(AvailableDrops[Random.Range(0, AvailableDrops.Count)]);
        }
    }

    public void PickAction()
    {
        if (!IsServer)
            return;

        List<BaseAbility> validAbilities = ReturnListOfValidAbilities();    //This generates a list of valid abilities

        if (validAbilities.Count == 0)
        {
            Debug.LogWarning("Could not find any valid abilities");
            return;
        }

        BaseAbility chosenAbility = ChooseAbility(validAbilities);  //This chooses a random valid ability

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

    private List<BaseAbility> ReturnListOfValidAbilities()
    {
        List<BaseAbility> validAbilities = new();

        foreach (BaseAbility ability in unitController.Abilities)
        {
            if (ability.ManaCost > unitController.CurrentMana.Value)
                continue;

            if (GetValidTargets(ability).Count == 0)
                continue;

            validAbilities.Add(ability);
        }

        return validAbilities;
    }

    private List<UnitController> GetValidTargets(BaseAbility ability)
    {
        List<UnitController> targets =
        ability.TeamTargeting == Team.Enemy
        ? BattleManager.instance.FriendlyUnits
        : BattleManager.instance.EnemyUnits;

        targets = targets
        .Where(t => t.IsAlive.Value)
        .ToList();

        // if (ability.DamageType == DamageType.Heal)
        // {
        //     targets = targets
        //         .Where(t => t.CurrentHealth.Value < t.MaxHealth)
        //         .ToList();
        // }

        return targets;
    }

    private BaseAbility ChooseAbility(List<BaseAbility> validAbilities)
    {
        int totalWeight = 0;

        foreach (BaseAbility ability in validAbilities)
        {
            totalWeight += ability.ManaCost;
        }

        int roll = Random.Range(0, totalWeight);

        foreach (BaseAbility ability in validAbilities)
        {
            roll -= ability.ManaCost;

            if (roll < 0)
                return ability;
        }

        return validAbilities[0];
    }
}
