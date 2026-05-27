using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class EnemyController : NetworkBehaviour
{
    public UnitController unitController;
    public UnitController target;
    public List<UnitController> unitControllers = new();
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

        List<BaseAbility> validAbilities = ReturnListOfValidAbilities();

        if (validAbilities.Count == 0)
        {
            Debug.Log("No valid abilities found, ending turn");
            unitController.IsActiveTurn = false;
            TurnManager.OnActionPhaseCompleted?.Invoke(unitController);
            return;
        }

        BaseAbility chosenAbility = ChooseAbility(validAbilities);

        List<UnitController> validTargets = GetValidTargets(chosenAbility);

        target = validTargets[Random.Range(0, validTargets.Count)];

        unitControllers = ReturnTargetControllers(chosenAbility);
        ulong targetId = target.NetworkObjectId;

        //unitController.TryUseAbility(chosenAbility, target);
        PickActionClientRpc(unitController.GetAbilityIndex(chosenAbility), targetId);
    }

    [ClientRpc]
    public void PickActionClientRpc(int abilityIndex, ulong targetId)
    {
        unitController.TryUseAbility(abilityIndex, targetId);
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

        if (ability.DamageType == DamageType.Heal)
        {
            targets = targets
                .Where(t => t.CurrentHealth.Value < t.MaxHealth)
                .ToList();
        }

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

    private List<UnitController> ReturnTargetControllers(BaseAbility ability)
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

    
}
