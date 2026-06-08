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

        SendCombatRoomVisualsClientRpc();

        CurrentlyLoadedRuntimeRoomData = roomData;
        StartCoroutine(DelayCombatStart());
    }

    private IEnumerator DelayCombatStart()
    {
        yield return new WaitForSeconds(0.5f);
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
    public void SendCombatRoomVisualsClientRpc()
    {
        CurrentlyLoadedBackground = Instantiate(ProgressionManager.instance.CurrentAreaData.DefaultAreaBackground);

        UIManager.instance.EnableCombatUI();
    }

    public void ServerUnloadCombatRoom()
    {
        if (IsServer)
        {
            for (int i = BattleManager.instance.EnemyUnits.Count - 1; i >= 0; i--)
            {
                UnitController controller = BattleManager.instance.EnemyUnits[i];
                TurnManager.instance.RemoveUnitFromTurnEntries(controller);
                controller.GetComponent<NetworkObject>().Despawn();
                Destroy(controller.gameObject);
            }

            BattleManager.instance.RemoveAllEnemies();

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

        foreach (UnitController controller in BattleManager.instance.AllUnits)
        {
            TurnManager.instance.RemoveTurnEntryUI(controller);
        }


        Destroy(CurrentlyLoadedBackground);
        CurrentlyLoadedBackground = null;

        if (!IsServer)
            BattleManager.instance.RemoveAllEnemies();

        CurrentlyLoadedRuntimeRoomData = null;
        OnCombatRoomUnloaded?.Invoke();
    }
}
