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

    public UnitController CurrentTurnUnitController;
    public ulong CurrentTurnUnitId;

    public UnitController LocalCurrentTurnUnitController;   //THIS IS JUST WHAT TURN THE CLIENT IS VIEWING

    public struct InitiativeEntry
    {
        public UnitController unit;
        public float roll;
    }

    private List<InitiativeEntry> initiativeList = new();

    public static Action<UnitController> OnRefreshUI;

    public static Action OnBattleEnded;
    public bool IsBattleEnded;
    public static Action OnActionSelected;

    public static Action OnLocalTurnProgressed;


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

    public void RemoveTurnEntryList(UnitController controller)
    {
        if (UnitControllerTurnOrder.Contains(controller))
        {
            UnitControllerTurnOrder.Remove(controller);
        }
    }

    public void RemoveTurnEntryUI(UnitController controller)
    {
        turnOrderPanelController.RemoveTurnEntry(controller);
    }

    private void GenerateTurnOrder()
    {
        if (!IsServer)
            return;

        initiativeList.Clear();
        IsBattleEnded = false;

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
            turnOrderPanelController.CreateTurnEntry(NetworkUtilities.GetUnitControllerById(unitIds[i]), rolls[i]);
        }
        turnOrderPanelController.SetActiveTurnEntry(NetworkUtilities.GetUnitControllerById(unitIds[0]));
        OnRefreshUI?.Invoke(NetworkUtilities.GetUnitControllerById(unitIds[0]));
    }

    public void MoveToNextTurn()
    {
        if (!IsServer)
            return;

        if (IsBattleOver())
        {
            IsBattleEnded = true; NotifyBattleEndedClientRpc();
            ProgressionManager.instance.DelayBeforeLoadingNextRoom();
            return;
        }

        if (CurrentTurnUnitController != null)
            CurrentTurnUnitController.ProcessEndTurnEffects();

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

        CurrentTurnUnitController = UnitControllerTurnOrder[CurrentTurnIndex];
        CurrentTurnUnitId = UnitControllerTurnOrder[CurrentTurnIndex].NetworkObjectId;

        NotifyTurnChangedClientRpc(CurrentTurnUnitId);
        CurrentTurnUnitController.RegenerateResources();
        CurrentTurnUnitController.ProcessStatusEffects();
        CurrentTurnUnitController.ServerBeginTurn();
    }

    [ClientRpc]
    void NotifyBattleEndedClientRpc()
    {
        CombatManager.instance.FinishQueuedActionsThenEndBattle();
    }

    [ClientRpc]
    void NotifyTurnChangedClientRpc(ulong unitTurnId)
    {
        CurrentTurnUnitId = unitTurnId;
        CurrentTurnUnitController = NetworkUtilities.GetUnitControllerById(unitTurnId);
        if (LocalCurrentTurnUnitController == null)
            LocalCurrentTurnUnitController = CurrentTurnUnitController;
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

    public bool IsUnitsTurn(ulong unitId)
    {
        return CurrentTurnUnitController.NetworkObjectId == unitId;
    }
}
