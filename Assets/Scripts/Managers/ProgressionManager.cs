using System;
using System.Collections.Generic;
using UnityEngine;

public class ProgressionManager : MonoBehaviour
{
    //Progression manager handles moving from scene to scene
    //This includes loading in the rooms, event menus, or camps

    public static ProgressionManager instance;

    public static RoomData CurrentRoomData;
    private GameObject CurrentlyLoadedBackground;

    public static Action OnRoomLoaded;

    public List<RoomData> RoomData;
    private List<RoomData> RemainingRoomData = new();

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

    public void LoadRoom(RoomData roomData)
    {
        if (CurrentRoomData != null)
            UnloadRoom();

        switch (roomData)
        {
            case CombatRoom combatRoom:
                CurrentlyLoadedBackground = Instantiate(combatRoom.RoomBackground);
                foreach (UnitController controller in combatRoom.Enemies)
                {
                    SpawnManager.instance.SpawnUnit(controller);
                }
                break;

            case EventRoom eventRoom:
                CurrentlyLoadedBackground = Instantiate(eventRoom.RoomMenu);
                break;

            default:
                break;
        }

        CurrentRoomData = roomData;
        OnRoomLoaded?.Invoke();
    }

    public void UnloadRoom()
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

        RoomData roomDataToLoad = RemainingRoomData[0];
        LoadRoom(roomDataToLoad);
        RemainingRoomData.RemoveAt(0);
    }
}
