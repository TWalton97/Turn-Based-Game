using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ProgressionManager : NetworkBehaviour
{
    public static ProgressionManager instance;

    public static Action OnRoomLoaded;

    public int RoomIndex = -1;
    public TextMeshProUGUI RoomCountText;

    private HashSet<ulong> readyClients = new();

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

    private NetworkVariable<int> CurrentRoomIndex = new(0);

    public List<UnitController> AvailableEnemies;

    public NetworkVariable<bool> IsTransitioning = new(false);
    public float transitionStartingTime;
    public const float MIN_TRANSITION_TIME = 1f;

    public List<AreaDataSO> AreaDataSOs;
    public AreaDataSO CurrentAreaData;
    private int currentAreaIndex = 0;

    public GameObject FirstLoadCanvas;

    public void Awake()
    {
        if (instance == null)
            instance = this;

        IsTransitioning.OnValueChanged += OnTransitionChanged;

        //We populate with the first area information
        LoadNextArea();

    }

    public void LoadNextArea()
    {
        if (AreaDataSOs[currentAreaIndex] == null)
        {
            Debug.LogWarning($"No area data for area {currentAreaIndex}");
            return;
        }
        CurrentAreaData = AreaDataSOs[currentAreaIndex];
        AvailableEnemies = AreaDataSOs[currentAreaIndex].AvailableEnemies;
        RoomCountText.text = $"{CurrentAreaData.AreaName} ({CurrentRoomIndex.Value}/{CurrentAreaData.NumberOfRooms})";
    }

    public void LoadFirstRoom()
    {
        StartCoroutine(TransitionToNextRoom());
        DisableFirstLoadCanvasClientRpc();
    }

    [ClientRpc]
    private void DisableFirstLoadCanvasClientRpc()
    {
        Invoke(nameof(DisableFirstLoadCanvas), 1f);
    }

    private void DisableFirstLoadCanvas()
    {
        FirstLoadCanvas.SetActive(false);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        IsTransitioning.OnValueChanged += OnTransitionChanged;
    }

    public void OnTransitionChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            UIManager.instance.EnableTravelingUI();
            StartCoroutine(DelayClientUnload());
        }
        else
        {
            UIManager.instance.EnableTravelingUI();
            RoomCountText.text = $"{CurrentAreaData.AreaName} ({CurrentRoomIndex.Value}/{CurrentAreaData.NumberOfRooms})";
        }
    }

    public IEnumerator DelayClientUnload()
    {
        yield return new WaitForSeconds(1f);
        CombatRoomManager.instance.ClientUnloadCombatRoom();
    }

    public IEnumerator TransitionToNextRoom()
    {
        if (!IsServer)
            yield break;

        IsTransitioning.Value = true;

        yield return new WaitForSeconds(1f);

        CombatRoomManager.instance.ServerUnloadCombatRoom();

        yield return new WaitForSeconds(1f);

        LoadNextRoom();
        IsTransitioning.Value = false;

        yield return null;
    }

    public void LoadNextRoom()
    {
        if (!IsServer)
            return;


        int roomsInArea = CurrentAreaData.NumberOfRooms;

        // -------------------------
        // BOSS + AREA TRANSITION
        // -------------------------
        if (CurrentRoomIndex.Value >= roomsInArea)
        {
            currentAreaIndex++;
            LoadNextAreaClientRpc(currentAreaIndex);

            CurrentRoomIndex.Value = 0;
            RoomIndex = -1;

            CampManager.instance.LoadCamp();
            IsTransitioning.Value = false;
            return;
        }

        bool isBossRoom = CurrentRoomIndex.Value == roomsInArea - 1;

        if (isBossRoom)
        {
            CombatRoomManager.instance.LoadCombatRoom(
                CombatRoomManager.instance.ReturnRoomDataAsRuntime(CurrentAreaData.BossRoomData)
            );

            CurrentRoomIndex.Value++;
            RoomIndex = -1; // important: not 0

            IsTransitioning.Value = false;
            return;
        }

        // -------------------------
        // NORMAL ROOM FLOW
        // -------------------------
        int nextRoomIndex = (RoomIndex + 1) % RoomOrder.Count;
        RoomType nextRoomType = RoomOrder[nextRoomIndex];

        switch (nextRoomType)
        {
            case RoomType.Camp:
                CampManager.instance.LoadCamp();
                break;

            case RoomType.Combat:
                CombatRoomManager.instance.LoadRandomCombatRoom(CurrentRoomIndex.Value, AvailableEnemies);
                break;

            case RoomType.Event:
                EventManager.instance.LoadEvent();
                break;
        }

        RoomIndex = nextRoomIndex;

        if (RoomIndex == 0)
            CurrentRoomIndex.Value++;

        IsTransitioning.Value = false;
    }

    [ClientRpc]
    public void LoadNextAreaClientRpc(int areaIndex)
    {
        currentAreaIndex = areaIndex;
        LoadNextArea();
    }

    public void ForceLoadCombatRoom(RuntimeRoomData runtimeRoomData)
    {
        CombatRoomManager.instance.LoadCombatRoom(runtimeRoomData);
    }

    [ServerRpc(RequireOwnership = false)]
    public void BattlePresentationFinishedServerRpc(ServerRpcParams rpcParams = default)
    {
        if (IsTransitioning.Value)
            return;

        ulong sender = rpcParams.Receive.SenderClientId;

        readyClients.Add(sender);

        if (AllClientsReady())
        {
            StartCoroutine(TransitionToNextRoom());

            foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
            {
                controller.CombatEndReset();
            }

            CombatEndResetClientRpc();
        }
    }

    [ClientRpc]
    private void CombatEndResetClientRpc()
    {
        foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
        {
            controller.CombatEndReset();
        }
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
}

