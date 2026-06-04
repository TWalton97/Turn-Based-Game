using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ProgressionManager : NetworkBehaviour
{
    public static ProgressionManager instance;

    public static RuntimeRoomData CurrentRoomData;
    private GameObject CurrentlyLoadedBackground;

    public static Action OnRoomLoaded;
    public static Action OnRoomCompleted;

    public List<RoomData> RoomData;
    private List<RoomData> RemainingRoomData = new();

    public int RoomIndex = -1;
    private int TotalCombatRoomsCompleted = 0;
    public TextMeshProUGUI RoomCountText;
    public GameObject RoomBackground;

    public enum RoomType
    {
        Camp,
        Combat,
        Event,
    }

    public List<RoomType> RoomOrder = new List<RoomType>
    {
        RoomType.Combat,
        RoomType.Camp,
        RoomType.Event,
        RoomType.Camp,
    };

    public int CurrentRoomIndex = 1;

    public List<UnitController> AvailableEnemies;
    public GameObject Background;

    private HashSet<ulong> readyClients = new();

    public NetworkVariable<int> NumberOfReadyVotes;
    bool hasVoted = false;

    public void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        RemainingRoomData = RoomData;
        TurnManager.OnBattleEnded += LoadNextRoom;
    }

    public void LoadFirstRoom()
    {
        LoadNextRoom();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        TurnManager.OnBattleEnded -= LoadNextRoom;
    }

    public void LoadEvent()
    {
        if (CurrentRoomData != null)
            UnloadCombatRoom();

        UIManager.instance.EnableEventUI();
        EventManager.instance.PopulateEventOptions();
    }

    public void LoadCamp()
    {
        if (CurrentRoomData != null)
            UnloadCombatRoom();

        hasVoted = false;

        if (IsServer)
            NumberOfReadyVotes.Value = 0;

        foreach (UnitController unit in BattleManager.instance.FriendlyUnits)
        {
            if (unit.IsOwner)
            {
                CampManager.instance.PopulateCampUI(unit);
                UIManager.instance.EnableCampUI();
                return;
            }
        }
    }

    public void LoadCombatRoomData(RoomData roomData)
    {
        CombatRoom combatRoom = roomData as CombatRoom;

        RuntimeRoomData runtimeRoomData = new();
        runtimeRoomData.presetRoomId = combatRoom.presetId;
        runtimeRoomData.Background = combatRoom.RoomBackground;
        runtimeRoomData.Enemies = combatRoom.Enemies;

        LoadCombatRoom(runtimeRoomData);
    }

    public void LoadCombatRoom(RuntimeRoomData roomData)
    {
        UIManager.instance.EnableCombatUI();

        CombatRoom combatRoom;
        RoomPresetDatabase.instance.TryGetRoomById(roomData.presetRoomId, out combatRoom);
        CurrentlyLoadedBackground = Instantiate(combatRoom.RoomBackground);


        if (NetworkManager.Singleton.IsServer)
        {
            if (roomData.Enemies.Count > 0)
            {
                foreach (CombatRoomEnemyEntry entry in roomData.Enemies)
                {
                    SpawnManager.instance.SpawnUnit(entry.Unit, entry.Level);
                }
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    SpawnManager.instance.SpawnUnit(AvailableEnemies[UnityEngine.Random.Range(0, AvailableEnemies.Count)], TotalCombatRoomsCompleted);
                }
            }
        }

        CurrentRoomData = roomData;
        OnRoomLoaded?.Invoke();
    }

    public void UnloadCombatRoom()
    {
        if (CurrentlyLoadedBackground != null)
        {
            Destroy(CurrentlyLoadedBackground);
            CurrentlyLoadedBackground = null;
        }

        OnRoomCompleted?.Invoke();

        if (NetworkManager.Singleton.IsServer)
        {
            foreach (UnitController controller in BattleManager.instance.EnemyUnits)
            {
                TurnManager.instance.RemoveUnitFromTurnEntries(controller);
                Destroy(controller.gameObject);
            }

            foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
            {
                TurnManager.instance.RemoveUnitFromTurnEntries(controller);
            }
        }

        TotalCombatRoomsCompleted++;
        BattleManager.instance.RemoveAllEnemies();
        CurrentRoomData = null;
    }

    public IEnumerator DelayBeforeLoadingNextRoom()
    {
        yield return new WaitForSeconds(1.5f);
        LoadNextRoom();
        yield return null;
    }

    public void LoadNextRoom()
    {
        if (CurrentRoomData != null)
            UnloadCombatRoom();

        if (NetworkManager.Singleton.IsServer)
        {
            foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
            {
                controller.ServerHeal(15, true);
            }

            if (RemainingRoomData.Count == 0)
            {
                Debug.Log("No more rooms remaining!");
                return;
            }

            RoomIndex = (RoomIndex + 1) % RoomOrder.Count;
            RoomType nextRoomType = RoomOrder[RoomIndex];

            //Picking what room is next
            switch (nextRoomType)
            {
                case RoomType.Camp:
                    LoadRoomClientRpc(0);
                    break;
                case RoomType.Combat:
                    LoadRoomClientRpc(1);
                    break;
                case RoomType.Event:
                    LoadRoomClientRpc(2);
                    break;
            }

            CurrentRoomIndex++;
            RoomCountText.text = "Forest (" + CurrentRoomIndex.ToString() + "/8)";
        }
    }

    [ClientRpc]
    public void LoadRoomClientRpc(int roomType)
    {
        switch (roomType)
        {
            case 0:
                LoadCamp();
                break;
            case 1:
                RuntimeRoomData roomDataToLoad = GenerateRoomData();
                LoadCombatRoom(roomDataToLoad);
                break;
            case 2:
                LoadEvent();
                break;
        }
    }

    public RuntimeRoomData GenerateRoomData()
    {
        RuntimeRoomData runtimeRoomData = new();

        runtimeRoomData.Background = RoomBackground;
        int roomBudget = 7 + (CurrentRoomIndex * 3);
        int enemyCount = 2;
        if (roomBudget >= 18)
        {
            enemyCount = UnityEngine.Random.value < 0.8f ? 2 : 3;
        }
        int enemyBudget = roomBudget / enemyCount;
        List<CombatRoomEnemyEntry> validEnemies = ReturnValidEnemies(enemyBudget);
        for (int i = 0; i < enemyCount; i++)
        {
            int rand = UnityEngine.Random.Range(0, validEnemies.Count);
            runtimeRoomData.Enemies.Add(validEnemies[rand]);
        }
        return runtimeRoomData;
    }

    public List<CombatRoomEnemyEntry> ReturnValidEnemies(int strengthPerEnemy)
    {
        List<CombatRoomEnemyEntry> validRoomEnemyEntries = new();

        foreach (UnitController controller in AvailableEnemies)
        {
            EnemyController enemyController = controller.GetComponent<EnemyController>();
            if (enemyController == null)
                continue;

            if (enemyController.baseStrength > strengthPerEnemy)
                continue;

            int maxLevel = GetMaxLevelForTarget(enemyController, strengthPerEnemy);
            CombatRoomEnemyEntry entry = new();
            entry.Unit = controller;
            entry.Level = maxLevel;
            validRoomEnemyEntries.Add(entry);
        }

        return validRoomEnemyEntries;
    }

    int GetMaxLevelForTarget(EnemyController enemy, int targetStrength)
    {
        int raw = targetStrength - enemy.baseStrength + 1;
        Debug.Log($"Max level for {enemy} is {raw}");
        return Mathf.Max(1, raw);
    }

    [ServerRpc(RequireOwnership = false)]
    public void BattlePresentationFinishedServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        readyClients.Add(sender);

        if (AllClientsReady())
        {
            LoadNextRoom();
        }
    }

    public void VoteReady()
    {
        if (!hasVoted)
        {
            hasVoted = true;
            RequestCampReadyVoteServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestCampReadyVoteServerRpc(ServerRpcParams rpcParams = default)
    {
        NumberOfReadyVotes.Value++;
        CountReadyVotes();
    }

    private void CountReadyVotes()
    {
        if (!IsServer)
            return;

        if (NumberOfReadyVotes.Value == NetworkManager.Singleton.ConnectedClients.Count)
            LoadNextRoom();
    }


    private bool AllClientsReady()
    {
        return readyClients.Count == NetworkManager.Singleton.ConnectedClients.Count;
    }
}

public class RuntimeRoomData
{
    public int presetRoomId = -1;
    public List<CombatRoomEnemyEntry> Enemies = new();
    public GameObject Background;

}
