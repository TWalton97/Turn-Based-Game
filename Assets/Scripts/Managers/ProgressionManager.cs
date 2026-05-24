using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProgressionManager : MonoBehaviour
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

    public void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        RemainingRoomData = RoomData;
        LoadNextRoom();
        TurnManager.OnBattleEnded += LoadNextRoom;
    }

    private void OnDestroy()
    {
        TurnManager.OnBattleEnded -= LoadNextRoom;
    }

    public void LoadEvent()
    {
        if (CurrentRoomData != null)
            UnloadCombatRoom();

        UIManager.instance.EnableEventUI();
        EventManager.instance.PopulateEventOptions();
    }

    public void LoadCombatRoom(RoomData roomData)
    {
        if (CurrentRoomData != null)
            UnloadCombatRoom();

        UIManager.instance.EnableCombatUI();

        switch (roomData)
        {
            case CombatRoom combatRoom:
                CurrentlyLoadedBackground = Instantiate(combatRoom.RoomBackground);
                foreach (UnitController controller in combatRoom.Enemies)
                {
                    SpawnManager.instance.SpawnUnit(controller);
                }
                break;
        }

        CurrentRoomData = roomData;
        OnRoomLoaded?.Invoke();
    }

    public void UnloadCombatRoom()
    {
        if (CurrentlyLoadedBackground != null)
            Destroy(CurrentlyLoadedBackground);

        foreach (UnitController controller in BattleManager.instance.EnemyUnits)
        {
            TurnManager.instance.RemoveUnitFromTurnEntries(controller);
            Destroy(controller.gameObject);
        }

        foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
        {
            TurnManager.instance.RemoveUnitFromTurnEntries(controller);
        }

        BattleManager.instance.RemoveAllEnemies();
        CurrentRoomData = null;
    }

    public void LoadNextRoom()
    {
        if (RemainingRoomData.Count == 0)
        {
            Debug.Log("No more rooms remaining!");
            return;
        }

        //When this gets called, we check our current index, then decide what to load based on index
        RoomIndex = (RoomIndex + 1) % RoomOrder.Count;
        RoomType nextRoomType = RoomOrder[RoomIndex];

        switch (nextRoomType)
        {
            case RoomType.Camp:
                LoadEvent();
                break;
            case RoomType.Combat:
                RoomData roomDataToLoad = RemainingRoomData[0];
                LoadCombatRoom(roomDataToLoad);
                RemainingRoomData.RemoveAt(0);
                break;
            case RoomType.Event:
                LoadEvent();
                break;
        }

        CurrentRoomIndex++;
        RoomCountText.text = "Forest (" + CurrentRoomIndex.ToString() + "/8)";
    }
}
