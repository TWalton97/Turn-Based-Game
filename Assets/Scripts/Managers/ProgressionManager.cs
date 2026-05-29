using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ProgressionManager : NetworkBehaviour
{
    //Progression manager handles moving from scene to scene
    //This includes loading in the rooms, event menus, or camps
    //Combat -> Camp -> Event -> Camp

    public static ProgressionManager instance;

    public static RoomData CurrentRoomData;
    private GameObject CurrentlyLoadedBackground;

    public static Action OnRoomLoaded;

    public List<RoomData> RoomData;
    private List<RoomData> RemainingRoomData = new();

    public int RoomIndex = -1;
    public TextMeshProUGUI RoomCountText;

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

    public void LoadCombatRoom(RoomData roomData)
    {
        if (CurrentRoomData != null)
            UnloadCombatRoom();

        UIManager.instance.EnableCombatUI();

        CurrentlyLoadedBackground = Instantiate(Background);

        if (NetworkManager.Singleton.IsServer)
        {
            for (int i = 0; i < 2; i++)
            {
                SpawnManager.instance.SpawnUnit(AvailableEnemies[UnityEngine.Random.Range(0, AvailableEnemies.Count)]);
            }
        }

        CurrentRoomData = roomData;
        OnRoomLoaded?.Invoke();
    }

    public void UnloadCombatRoom()
    {
        if (CurrentlyLoadedBackground != null)
            Destroy(CurrentlyLoadedBackground);

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
                    //LoadCamp();
                    break;
                case RoomType.Combat:
                    LoadRoomClientRpc(1);
                    //RoomData roomDataToLoad = RemainingRoomData[0];
                    //LoadCombatRoom(roomDataToLoad);
                    break;
                case RoomType.Event:
                    LoadRoomClientRpc(2);
                    //LoadEvent();
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
                RoomData roomDataToLoad = RemainingRoomData[0];
                LoadCombatRoom(roomDataToLoad);
                break;
            case 2:
                LoadEvent();
                break;
        }
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

    private bool AllClientsReady()
    {
        return readyClients.Count == NetworkManager.Singleton.ConnectedClients.Count;
    }
}
