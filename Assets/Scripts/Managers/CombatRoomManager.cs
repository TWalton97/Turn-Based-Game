using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CombatRoomManager : NetworkBehaviour
{
    public static CombatRoomManager instance;

    public RuntimeRoomData CurrentlyLoadedRuntimeRoomData;
    public GameObject CurrentlyLoadedBackground;

    public static Action OnCombatRoomLoaded;
    public static Action OnCombatRoomUnloaded;

    public void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    //This is a function for converting a RoomData to a RuntimeRoomData
    public RuntimeRoomData ReturnRoomDataAsRuntime(RoomData roomData)
    {
        if (roomData is not CombatRoom combatRoom)
        {
            Debug.LogWarning($"Trying to load a non-combat room!");
            return null;
        }

        RuntimeRoomData runtimeRoomData = new();
        runtimeRoomData.presetRoomId = combatRoom.presetId;
        runtimeRoomData.Enemies = combatRoom.Enemies;

        return runtimeRoomData;
    }

    public void LoadCombatRoom(RuntimeRoomData roomData)
    {
        if (!IsServer)
            return;

        foreach (CombatRoomEnemyEntry entry in roomData.Enemies)
        {
            SpawnManager.instance.SpawnUnit(entry.Unit, entry.Level);
        }

        SendCombatRoomVisualsClientRpc(roomData.presetRoomId);

        CurrentlyLoadedRuntimeRoomData = roomData;
        StartCoroutine(DelayCombatStart());
    }

    private IEnumerator DelayCombatStart()
    {
        yield return new WaitForSeconds(1.5f);
        OnCombatRoomLoaded?.Invoke();
    }

    public void LoadRandomCombatRoom(int currentRoomIndex, List<UnitController> allAreaEnemies)
    {
        if (!IsServer)
            return;

        RuntimeRoomData runtimeRoomData = RoomBuilder.GenerateRoomData(currentRoomIndex, allAreaEnemies);
        LoadCombatRoom(runtimeRoomData);
    }

    [ClientRpc]
    public void SendCombatRoomVisualsClientRpc(int roomPresetId)
    {
        CombatRoom combatRoom;
        RoomPresetDatabase.instance.TryGetRoomById(roomPresetId, out combatRoom);
        CurrentlyLoadedBackground = Instantiate(combatRoom.RoomBackground);

        UIManager.instance.EnableCombatUI();
    }

    public void ServerUnloadCombatRoom()
    {
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
    }

    public void ClientUnloadCombatRoom()
    {
        if (CurrentlyLoadedBackground == null)
            return;

        Destroy(CurrentlyLoadedBackground);
        CurrentlyLoadedBackground = null;

        BattleManager.instance.RemoveAllEnemies();

        CurrentlyLoadedRuntimeRoomData = null;
        OnCombatRoomUnloaded?.Invoke();
    }
}
