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
        RuntimeAbilityInstance ability = user.RuntimeAbilityInstances[abilityIndex];

        if (!ability.CanUse(user))
        {
            Debug.LogWarning($"UnitController with id {userId} does not have the resources to use ability {ability.Ability.AbilityName}");
            return;
        }

        ServerResolveTargets(userId, abilityIndex, targetId);
    }

    public void ServerResolveTargets(ulong userId, int abilityIndex, ulong targetId)
    {
        UnitController user = NetworkUtilities.GetUnitControllerById(userId);
        RuntimeAbilityInstance ability = user.RuntimeAbilityInstances[abilityIndex];
        UnitController target = NetworkUtilities.GetUnitControllerById(targetId);

        ServerExecuteAbility(user, ability, target);
    }

    public void ServerExecuteAbility(UnitController user, RuntimeAbilityInstance ability, UnitController target)
    {
        if (!IsServer)
            return;

        ability.ConsumeCost(user);

        AbilityResult abilityResult = CombatResolver.ResolveAbility(user, ability.Ability, target);

        foreach (AbilityEffectResult abilityEffectResult in abilityResult.AbilityEffectResults)
        {
            foreach (TargetResult targetResult in abilityEffectResult.TargetResults)
            {
                UnitController controller = NetworkUtilities.GetUnitControllerById(targetResult.TargetId);
                foreach (HitResult hitResult in targetResult.Hits)
                {
                    controller.ServerTakeDamage(hitResult);
                }
            }
        }

        BeginAnimationSequenceClientRpc(abilityResult);   //This is just telling each client to play the animation
        TurnManager.instance.ServerMoveToNextTurn();
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
            yield return new WaitUntil(() => TurnManager.instance.ClientCurrentTurnUnitController == user && user.ActionPhaseStarted);
            yield return user.PlayAbilitySequence(user.RuntimeAbilityInstances[result.AbilityId].Ability, result);
        }

        isPlayingAbility = false;
    }

    public void FinishQueuedActionsThenEndBattle()
    {
        StartCoroutine(EndBattleRoutine());
    }

    private IEnumerator EndBattleRoutine()
    {
        while (isPlayingAbility)
            yield return null;

        yield return new WaitForSeconds(1f);
        
        ProgressionManager.instance.BattlePresentationFinishedServerRpc();
    }

}
