using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    //This script controls spawning of players and units

    public static SpawnManager instance;

    public UnitController DEBUG_PlayerHealer;
    public UnitController DEBUG_PlayerWarrior;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void BindNetworkEvents()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        SpawnUnitForPlayer(clientId);
    }

    private void SpawnUnitForPlayer(ulong clientId)
    {
        BattleSlot slot = BattleManager.instance.ReturnEmptyBattleSlotOfType(DEBUG_PlayerWarrior.UnitTeam);

        GameObject unitObj = Instantiate(DEBUG_PlayerWarrior.gameObject);

        NetworkObject netObj = unitObj.GetComponent<NetworkObject>();

        unitObj.transform.position = slot.UnitHolder.position;

        netObj.SpawnWithOwnership(clientId);

        UnitController unit = unitObj.GetComponent<UnitController>();

        BattleManager.instance.RegisterUnit(unit);
        slot.BindUnitToSlot(unit);
    }

    public void SpawnUnit(UnitController unit)
    {
        BattleSlot slot = BattleManager.instance.ReturnEmptyBattleSlotOfType(unit.UnitTeam);
        UnitController controller = Instantiate(unit, slot.UnitHolder);
        BattleManager.instance.RegisterUnit(controller);
        slot.BindUnitToSlot(controller);
    }
}
