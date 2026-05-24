using System;
using System.Collections;
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

    public RoomData DEBUG_RoomDataToLoad;

    public void Awake()
    {
        if (instance == null)
            instance = this;
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
            BattleManager.instance.UnregisterUnit(controller);
            Destroy(controller.gameObject);
        }

        CurrentRoomData = null;
    }

    [ContextMenu("Load Room")]
    public void LoadDebugRoomData()
    {
        LoadRoom(DEBUG_RoomDataToLoad);
    }
}
