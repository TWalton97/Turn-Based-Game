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

    public bool ServerIsBattleEnded;
    public bool ClientIsBattleEnded;
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
        CombatRoomManager.OnCombatRoomLoaded += GenerateTurnOrder;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        ProgressionManager.OnRoomLoaded -= GenerateTurnOrder;
        CombatRoomManager.OnCombatRoomLoaded -= GenerateTurnOrder;
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

        ServerCurrentTurnIndex = -1;
        initiativeList.Clear();
        ServerCurrentTurnUnitController = null;
        ServerIsBattleEnded = false;

        foreach (var unit in BattleManager.instance.FriendlyUnits)
        {
            initiativeList.Add(new InitiativeEntry
            {
                unit = unit,
                roll = Mathf.Round(UnityEngine.Random.Range(unit.GetStatType(StatType.InitiativeMin), unit.GetStatType(StatType.InitiativeMax)) * 10f) / 10f
            });
        }

        foreach (var unit in BattleManager.instance.EnemyUnits)
        {
            initiativeList.Add(new InitiativeEntry
            {
                unit = unit,
                roll = Mathf.Round(UnityEngine.Random.Range(unit.GetStatType(StatType.InitiativeMin), unit.GetStatType(StatType.InitiativeMax)) * 10f) / 10f
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

        ServerMoveToNextTurn();
        GenerateTurnEntryListClientRpc(unitIds, rolls, ServerCurrentTurnIndex);
    }

    [ClientRpc]
    public void GenerateTurnEntryListClientRpc(ulong[] unitIds, float[] rolls, int serverCurrentTurnIndex)
    {
        ClientCurrentTurnIndex = -1;
        initiativeList.Clear();
        ClientCurrentTurnUnitController = null;

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
        turnOrderPanelController.SetActiveTurnEntry(NetworkUtilities.GetUnitControllerById(unitIds[serverCurrentTurnIndex]));
        ClientMoveToNextTurn();
    }

    public void ServerMoveToNextTurn()
    {
        if (!IsServer)
            return;

        if (ServerIsBattleOver())
        {
            ServerIsBattleEnded = true;
            NotifyBattleEndedClientRpc();
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
        while (!UnitControllerTurnOrder[nextIndex].ServerIsAlive.Value);

        ServerCurrentTurnIndex = nextIndex;
        ServerCurrentTurnUnitController = UnitControllerTurnOrder[nextIndex];
        ServerCurrentTurnUnitId = UnitControllerTurnOrder[ServerCurrentTurnIndex].NetworkObjectId;

        OnServerTurnStarted?.Invoke(ServerCurrentTurnUnitController);
        OnServerActionPhaseStarted?.Invoke(ServerCurrentTurnUnitController);
    }

    public void RequestClientTurnAdvance()
    {
        StartCoroutine(ClientAdvanceRoutine());
    }

    private IEnumerator ClientAdvanceRoutine()
    {
        yield return new WaitForSeconds(1f);

        ClientMoveToNextTurn();
    }

    private void ClientMoveToNextTurn()
    {
        if (ClientIsBattleOver())
        {
            ClientIsBattleEnded = true;
            return;
        }

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
        while (!UnitControllerTurnOrder[nextIndex].ClientIsAlive);

        ClientCurrentTurnIndex = nextIndex;
        ClientCurrentTurnUnitController = UnitControllerTurnOrder[ClientCurrentTurnIndex];
        ClientCurrentTurnUnitId = UnitControllerTurnOrder[ClientCurrentTurnIndex].NetworkObjectId;

        turnOrderPanelController.SetActiveTurnEntry(ClientCurrentTurnUnitController);

        OnClientTurnStarted?.Invoke(ClientCurrentTurnUnitController);
    }

    [ClientRpc]
    void NotifyBattleEndedClientRpc()
    {
        CombatManager.instance.FinishQueuedActionsThenEndBattle();
    }

    private bool ServerIsBattleOver()
    {
        bool allFriendliesDead = BattleManager.instance.FriendlyUnits.All(u => !u.ServerIsAlive.Value);

        bool allEnemiesDead = BattleManager.instance.EnemyUnits.All(u => !u.ServerIsAlive.Value);

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

    private bool ClientIsBattleOver()
    {
        bool allFriendliesDead = BattleManager.instance.FriendlyUnits.All(u => !u.ClientIsAlive);

        bool allEnemiesDead = BattleManager.instance.EnemyUnits.All(u => !u.ClientIsAlive);

        if (allFriendliesDead)
        {
            return true;
        }

        if (allEnemiesDead)
        {
            return true;
        }

        return false;
    }

    public bool IsUnitsTurn(ulong unitId)
    {
        return ServerCurrentTurnUnitController.NetworkObjectId == unitId;
    }
}
