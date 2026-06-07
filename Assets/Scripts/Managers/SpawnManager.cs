using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : NetworkBehaviour
{
    //This script controls spawning of players and units

    public static SpawnManager instance;

    public UnitController WarriorPrefab;
    public UnitController RoguePrefab;
    public UnitController PriestPrefab;
    public UnitController DuelistPrefab;
    public UnitController WarlordPrefab;
    public UnitController ArcanistPrefab;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer)
            return;

        var playerLobbyStates = FindObjectsOfType<PlayerLobbyState>();
        foreach (var player in playerLobbyStates)
        {
            UnitController classPrefab = ReturnClassPrefab(player.playerClass.Value);
            SpawnUnitForPlayer(player.OwnerClientId, classPrefab, player.playerName.Value.ToString());
        }

        ProgressionManager.instance.LoadFirstRoom();
    }

    private void SpawnUnitForPlayer(ulong clientId, UnitController classPrefab, string name = null)
    {
        if (!IsServer)
            return;

        BattleSlot slot = BattleManager.instance.ReturnEmptyBattleSlotOfType(classPrefab.UnitTeam);

        GameObject unitObj = Instantiate(classPrefab.gameObject);

        NetworkObject netObj = unitObj.GetComponent<NetworkObject>();

        unitObj.transform.position = slot.UnitHolder.position;
        unitObj.transform.rotation = slot.UnitHolder.transform.rotation;

        UnitController unit = unitObj.GetComponent<UnitController>();

        if (!string.IsNullOrEmpty(name))
        {
            unit.UnitName = name;
        }
        else
        {
            unit.UnitName = classPrefab.UnitData.ClassName;
        }

        netObj.SpawnWithOwnership(clientId);

        unit.Level.Value = 1;

        unit.ApplyClassPresetStats();

        slot.BindUnitToSlot(unit);
    }

    public void SpawnUnit(UnitController unit, int level = 1)
    {
        if (!IsServer)
            return;

        BattleSlot slot = BattleManager.instance.ReturnEmptyBattleSlotOfType(unit.UnitTeam);
        UnitController controller = Instantiate(unit, slot.UnitHolder);

        NetworkObject netObj = controller.GetComponent<NetworkObject>();

        controller.transform.position = slot.UnitHolder.position;
        controller.transform.rotation = slot.UnitHolder.transform.rotation;

        netObj.Spawn();

        controller.Level.Value = level;

        controller.ApplyClassPresetStats();

        slot.BindUnitToSlot(controller);
    }

    public UnitController ReturnClassPrefab(PlayerClass playerClass)
    {
        switch (playerClass)
        {
            case PlayerClass.Warrior:
                return WarriorPrefab;
            case PlayerClass.Rogue:
                return RoguePrefab;
            case PlayerClass.Priest:
                return PriestPrefab;
            case PlayerClass.Duelist:
                return DuelistPrefab;
            case PlayerClass.Warlord:
                return WarlordPrefab;
            case PlayerClass.Arcanist:
                return ArcanistPrefab;
        }
        return null;
    }
}

public enum PlayerClass
{
    Warrior,
    Rogue,
    Priest,
    Duelist,
    Warlord,
    Arcanist
}
