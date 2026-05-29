using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager instance;

    public TurnOrderPanelController turnOrderPanelController;

    public List<UnitController> UnitControllerTurnOrder;

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

    //Server-side turn events
    public static Action<UnitController> OnServerTurnStarted;
    public static Action<UnitController> OnServerActionPhaseStarted;
    public static Action<UnitController> OnServerTurnEnded;
    public int ServerCurrentTurnIndex;
    public UnitController ServerCurrentTurnUnitController;
    public ulong ServerCurrentTurnUnitId;

    //Client-side turn events
    public static Action<UnitController> OnClientTurnStarted;
    public static Action<UnitController> OnClientActionPhaseStarted;
    public static Action<UnitController> OnClientTurnEnded;
    public int ClientCurrentTurnIndex;
    public UnitController ClientCurrentTurnUnitController;
    public ulong ClientCurrentTurnUnitId;

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

        ServerCurrentTurnIndex = -1;
        ServerMoveToNextTurn();

        GenerateTurnEntryListClientRpc(unitIds, rolls);
    }

    [ClientRpc]
    public void GenerateTurnEntryListClientRpc(ulong[] unitIds, float[] rolls)
    {
        for (int i = 0; i < unitIds.Length; i++)
        {
            UnitController unit = NetworkUtilities.GetUnitControllerById(unitIds[i]);
            turnOrderPanelController.CreateTurnEntry(unit, rolls[i]);
            if (!IsServer)
            {
                initiativeList.Add(new InitiativeEntry
                {
                    unit = unit,
                    roll = rolls[i]
                });

                initiativeList = initiativeList.OrderByDescending(x => x.roll).ToList();
                UnitControllerTurnOrder = initiativeList.Select(x => x.unit).ToList();
            }

        }
        turnOrderPanelController.SetActiveTurnEntry(NetworkUtilities.GetUnitControllerById(unitIds[0]));
        ClientCurrentTurnIndex = -1;
        ClientMoveToNextTurn();
    }

    public void ServerMoveToNextTurn()
    {
        if (!IsServer)
            return;

        if (IsBattleOver())
        {
            IsBattleEnded = true; NotifyBattleEndedClientRpc();
            ProgressionManager.instance.DelayBeforeLoadingNextRoom();
            return;
        }

        UnitController previousUnit = ServerCurrentTurnUnitController;

        if (ServerCurrentTurnUnitController != null)
            OnServerTurnEnded?.Invoke(ServerCurrentTurnUnitController);

        int currentIndex = UnitControllerTurnOrder.IndexOf(previousUnit);

        int attempts = 0;
        int nextIndex = currentIndex;

        do
        {
            nextIndex = (nextIndex + 1) % UnitControllerTurnOrder.Count;

            attempts++;

            if (attempts >= UnitControllerTurnOrder.Count)
            {
                Debug.Log("No alive units remaining.");
                return;
            }
        }
        while (!UnitControllerTurnOrder[nextIndex].IsAlive.Value);

        ServerCurrentTurnIndex = nextIndex;
        ServerCurrentTurnUnitController = UnitControllerTurnOrder[nextIndex];
        ServerCurrentTurnUnitId = UnitControllerTurnOrder[ServerCurrentTurnIndex].NetworkObjectId;

        OnServerTurnStarted?.Invoke(ServerCurrentTurnUnitController);
        OnServerActionPhaseStarted?.Invoke(ServerCurrentTurnUnitController);
    }

    public void ClientMoveToNextTurn()
    {
        if (ClientCurrentTurnUnitController != null)
            OnClientTurnEnded?.Invoke(ClientCurrentTurnUnitController);

        UnitController previousUnit = ClientCurrentTurnUnitController;
        int currentIndex = UnitControllerTurnOrder.IndexOf(previousUnit);

        int attempts = 0;
        int nextIndex = currentIndex;

        do
        {
            nextIndex = (nextIndex + 1) % UnitControllerTurnOrder.Count;

            attempts++;

            if (attempts >= UnitControllerTurnOrder.Count)
            {
                Debug.Log("No alive units remaining.");
                return;
            }
        }
        while (!UnitControllerTurnOrder[nextIndex].IsAlive.Value);

        ClientCurrentTurnIndex = nextIndex;
        ClientCurrentTurnUnitController = UnitControllerTurnOrder[ClientCurrentTurnIndex];
        ClientCurrentTurnUnitId = UnitControllerTurnOrder[ClientCurrentTurnIndex].NetworkObjectId;

        turnOrderPanelController.SetActiveTurnEntry(ClientCurrentTurnUnitController);

        OnClientTurnStarted?.Invoke(ClientCurrentTurnUnitController);
        OnClientActionPhaseStarted?.Invoke(ClientCurrentTurnUnitController);
    }

    [ClientRpc]
    void NotifyBattleEndedClientRpc()
    {
        CombatManager.instance.FinishQueuedActionsThenEndBattle();
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
        return ServerCurrentTurnUnitController.NetworkObjectId == unitId;
    }
}
