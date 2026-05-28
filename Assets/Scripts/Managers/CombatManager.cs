using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class CombatManager : NetworkBehaviour
{
    public static CombatManager instance;

    private Queue<AbilityResult> abilityQueue = new();
    public bool isPlayingAbility;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    //Can be called from any UnitController to request using an ability
    [ServerRpc(RequireOwnership = false)]
    public void RequestCombatActionServerRpc(ulong userId, int abilityIndex, ulong targetId)
    {
        ServerValidateAbility(userId, abilityIndex, targetId);
    }


    public void ServerValidateAbility(ulong userId, int abilityIndex, ulong targetId)
    {
        if (!IsServer)
            return;

        if (!TurnManager.instance.IsUnitsTurn(userId))
        {
            Debug.LogWarning($"Unit with id {userId} is trying to perform an action but it is not their turn!");
            return;
        }

        UnitController user = NetworkUtilities.GetUnitControllerById(userId);
        BaseAbility ability = user.Abilities[abilityIndex];

        if (!ability.CanUse(user))
        {
            Debug.LogWarning($"UnitController with id {userId} does not have the resources to use ability {ability.name}");
            return;
        }

        ServerResolveTargets(userId, abilityIndex, targetId);
    }

    public void ServerResolveTargets(ulong userId, int abilityIndex, ulong targetId)
    {
        UnitController user = NetworkUtilities.GetUnitControllerById(userId);
        BaseAbility ability = user.Abilities[abilityIndex];

        List<UnitController> targets = new();

        switch (ability.TargetType)
        {
            case TargetType.SingleUnit:
                targets.Add(NetworkUtilities.GetUnitControllerById(targetId));
                break;
            case TargetType.AllUnits:
                if (ability.TeamTargeting == Team.Ally)
                {
                    targets = BattleManager.instance.AllUnits.Where(u => u.UnitTeam == user.UnitTeam && u.IsAlive.Value).ToList();
                }
                else
                {
                    targets = BattleManager.instance.AllUnits.Where(u => u.UnitTeam != user.UnitTeam && u.IsAlive.Value).ToList();
                }
                break;
            case TargetType.Self:
                targets.Add(user);
                break;
        }

        ServerExecuteAbility(user, ability, targets);
    }

    public void ServerExecuteAbility(UnitController user, BaseAbility ability, List<UnitController> targets)
    {
        if (!IsServer)
            return;

        AbilityResult abilityResult = CombatResolver.ResolveAbility(user, ability, targets);
        foreach (TargetResult targetResult in abilityResult.TargetResults)
        {
            UnitController controller = NetworkUtilities.GetUnitControllerById(targetResult.TargetId);
            for (int i = 0; i < targetResult.Hits.Length; i++)
            {
                controller.ServerTakeDamage(targetResult.Hits[i]);
            }
        }

        BeginAnimationSequenceClientRpc(abilityResult);   //This is just telling each client to play the animation
        TurnManager.instance.MoveToNextTurn();
    }

    [ClientRpc]
    public void BeginAnimationSequenceClientRpc(AbilityResult abilityResult)
    {
        UnitController user = NetworkUtilities.GetUnitControllerById(abilityResult.AttackerId);
        ClientPlayAbilitySequence(abilityResult);
        TurnManager.OnActionSelected?.Invoke();
    }

    public void ClientPlayAbilitySequence(AbilityResult abilityResult)
    {
        abilityQueue.Enqueue(abilityResult);

        if (!isPlayingAbility)
        {
            StartCoroutine(ProcessAbilityQueue());
        }
    }

    private IEnumerator ProcessAbilityQueue()
    {
        isPlayingAbility = true;


        while (abilityQueue.Count > 0)
        {
            AbilityResult result = abilityQueue.Dequeue();
            UnitController user = NetworkUtilities.GetUnitControllerById(result.AttackerId);
            TurnManager.instance.turnOrderPanelController.SetActiveTurnEntry(user);
            yield return user.PlayAbilitySequence(user.Abilities[result.AbilityId], result);
        }

        isPlayingAbility = false;
        TurnManager.instance.turnOrderPanelController.SetActiveTurnEntry(TurnManager.instance.CurrentTurnUnitController);
        TurnManager.OnRefreshUI?.Invoke(TurnManager.instance.CurrentTurnUnitController);
    }

    public void FinishQueuedActionsThenEndBattle()
    {
        StartCoroutine(EndBattleRoutine());
    }

    private IEnumerator EndBattleRoutine()
    {
        while (isPlayingAbility)
            yield return null;

        ProgressionManager.instance.BattlePresentationFinishedServerRpc();
    }

}
