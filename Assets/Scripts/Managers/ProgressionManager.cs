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

    public int CurrentRoomIndex = 1;

    public List<UnitController> AvailableEnemies;
    public GameObject Background;

    public NetworkVariable<bool> IsTransitioning = new(false);
    public float transitionStartingTime;
    public const float MIN_TRANSITION_TIME = 1f;

    public void Awake()
    {
        if (instance == null)
            instance = this;

        IsTransitioning.OnValueChanged += OnTransitionChanged;
    }

    public void LoadFirstRoom()
    {
        StartCoroutine(TransitionToNextRoom());
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
        }
    }

    public IEnumerator DelayClientUnload()
    {
        yield return new WaitForSeconds(1f);
        CombatRoomManager.instance.ClientUnloadCombatRoom();
    }

    public IEnumerator TransitionToNextRoom()
    {
        IsTransitioning.Value = true;

        yield return new WaitForSeconds(1.5f);

        CombatRoomManager.instance.ServerUnloadCombatRoom();

        yield return new WaitForSeconds(1.5f);

        LoadNextRoom();
        IsTransitioning.Value = false;

        yield return null;
    }

    public void LoadNextRoom()
    {
        if (!IsServer)
            return;

        RoomType nextRoomType = RoomOrder[(RoomIndex + 1) % RoomOrder.Count];

        //Picking what room is next
        switch (nextRoomType)
        {
            case RoomType.Camp:
                CampManager.instance.LoadCamp();
                break;
            case RoomType.Combat:
                CombatRoomManager.instance.LoadRandomCombatRoom(CurrentRoomIndex, AvailableEnemies);
                break;
            case RoomType.Event:
                EventManager.instance.LoadEvent();
                break;
        }

        //This is a looping index for iterating through our RoomOrder list
        RoomIndex = (RoomIndex + 1) % RoomOrder.Count;

        //This tracks the actual room index, goes up by 1 each room
        if (RoomIndex == 0)
            CurrentRoomIndex++;

        RoomCountText.text = "Forest (" + CurrentRoomIndex.ToString() + "/8)";
        IsTransitioning.Value = false;
    }

    public void ForceLoadCombatRoom(RuntimeRoomData runtimeRoomData)
    {
        CombatRoomManager.instance.LoadCombatRoom(runtimeRoomData);
    }

    [ServerRpc(RequireOwnership = false)]
    public void BattlePresentationFinishedServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        readyClients.Add(sender);

        if (AllClientsReady())
        {
            StartCoroutine(TransitionToNextRoom());

            foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
            {
                controller.ServerHeal(15, true);
                controller.CombatEndReset();
            }
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
