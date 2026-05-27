using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    //This class just handles figuring out the turn order
    //It also calls to specific units when their turn starts
    public static TurnManager instance;

    public TurnOrderPanelController turnOrderPanelController;

    public List<UnitController> UnitControllerTurnOrder;
    public int CurrentTurnIndex;

    public struct InitiativeEntry
    {
        public UnitController unit;
        public float roll;
    }

    private List<InitiativeEntry> initiativeList = new();

    public static Action<UnitController> OnRefreshUI;
    public static Action<UnitController> OnActionPhaseCompleted;

    public static Action OnTurnEnded;
    public static Action OnBattleEnded;


    //TODO: move this to a UI manager
    public CombatMenuController CombatMenuController;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        ProgressionManager.OnRoomLoaded += GenerateTurnOrder;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        ProgressionManager.OnRoomLoaded -= GenerateTurnOrder;
    }

    public void RemoveUnitFromTurnEntries(UnitController controller)
    {
        if (UnitControllerTurnOrder.Contains(controller))
        {
            turnOrderPanelController.RemoveTurnEntry(controller);
            UnitControllerTurnOrder.Remove(controller);
        }
    }

    private void GenerateTurnOrder()
    {
        if (!IsServer)
            return;

        initiativeList.Clear();

        foreach (var unit in BattleManager.instance.FriendlyUnits)
        {
            initiativeList.Add(new InitiativeEntry
            {
                unit = unit,
                roll = Mathf.Round(UnityEngine.Random.Range(unit.CombatStats.InitiativeMin, unit.CombatStats.InitiativeMax) * 10f) / 10f
            });
        }

        foreach (var unit in BattleManager.instance.EnemyUnits)
        {
            initiativeList.Add(new InitiativeEntry
            {
                unit = unit,
                roll = Mathf.Round(UnityEngine.Random.Range(unit.CombatStats.InitiativeMin, unit.CombatStats.InitiativeMax) * 10f) / 10f
            });
        }

        initiativeList = initiativeList.OrderByDescending(x => x.roll).ToList();
        UnitControllerTurnOrder = initiativeList.Select(x => x.unit).ToList();

        ulong[] unitIds = new ulong[initiativeList.Count];
        float[] rolls = new float[initiativeList.Count];

        for (int i = 0; i < initiativeList.Count; i++)
        {
            unitIds[i] = initiativeList[i].unit.NetworkObjectId;
            rolls[i] = initiativeList[i].roll;
        }

        GenerateTurnEntryListClientRpc(unitIds, rolls);

        CurrentTurnIndex = -1;
        MoveToNextTurn();
    }

    [ClientRpc]
    public void GenerateTurnEntryListClientRpc(ulong[] unitIds, float[] rolls)
    {
        for (int i = 0; i < unitIds.Length; i++)
        {
            turnOrderPanelController.CreateTurnEntry(GetTarget(unitIds[i]), rolls[i]);
        }
    }

    private void MoveToNextTurn()
    {
        if (!IsServer)
            return;

        if (IsBattleOver())
        {
            OnBattleEnded?.Invoke();
            return;
        }

        int attempts = 0;

        do
        {
            CurrentTurnIndex =
                (CurrentTurnIndex + 1) % UnitControllerTurnOrder.Count;

            attempts++;

            if (attempts >= UnitControllerTurnOrder.Count)
            {
                Debug.Log("No alive units remaining.");
                return;
            }
        }
        while (!UnitControllerTurnOrder[CurrentTurnIndex].IsAlive.Value);

        UnitController currentUnit =
            UnitControllerTurnOrder[CurrentTurnIndex];

        currentUnit.RegenerateResources();

        currentUnit.ProcessStatusEffects();

        StartTurnClientRpc(currentUnit.NetworkObjectId);

        currentUnit.BeginActionPhase();
    }

    [ClientRpc]
    public void StartTurnClientRpc(ulong unitId)
    {
        UnitController unit = GetTarget(unitId);
        CombatMenuController.CurrentUnitController = unit;

        turnOrderPanelController.SetActiveTurnEntry(unit);

        OnRefreshUI?.Invoke(unit);

        unit.BeginClientTurn();
    }

    public UnitController GetTarget(ulong targetId)
    {
        UnitController controller = BattleManager.instance.AllUnits.Find(t => t.NetworkObjectId == targetId);
        return controller;
    }

    public void ResolveAction(UnitController controller)
    {
        controller.ProcessEndTurnEffects();

        controller.EndClientTurn();

        MoveToNextTurn();
    }

    private bool IsBattleOver()
    {
        bool allFriendliesDead = BattleManager.instance.FriendlyUnits.All(u => !u.IsAlive.Value);

        bool allEnemiesDead = BattleManager.instance.EnemyUnits.All(u => !u.IsAlive.Value);

        if (allFriendliesDead)
        {
            Debug.Log("All friendlies dead");
            return true;
        }

        if (allEnemiesDead)
        {
            Debug.Log("All enemies dead");
            return true;
        }

        return false;
    }

    public void TryUseAbility_Server(int abilityIndex, ulong targetId)
    {

    }
}
