using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    //This script controls spawning of players and units

    public static SpawnManager instance;

    public UnitController DEBUG_PlayerHealer;
    public UnitController DEBUG_PlayerWarrior;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }

    public void HandleClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        SpawnUnitForPlayer(clientId);
    }

    public UnitController PickRandomClass()
    {
        int rand = Random.Range(0, 2);
        if (rand == 0)
        {
            return DEBUG_PlayerWarrior;
        }

        return DEBUG_PlayerHealer;
    }

    private void SpawnUnitForPlayer(ulong clientId)
    {
        if (!IsServer)
            return;

        BattleSlot slot = BattleManager.instance.ReturnEmptyBattleSlotOfType(DEBUG_PlayerWarrior.UnitTeam);

        GameObject unitObj = Instantiate(PickRandomClass().gameObject);

        NetworkObject netObj = unitObj.GetComponent<NetworkObject>();

        unitObj.transform.position = slot.UnitHolder.position;
        unitObj.transform.rotation = slot.UnitHolder.transform.rotation;

        UnitController unit = unitObj.GetComponent<UnitController>();
        
        netObj.SpawnWithOwnership(clientId);

        unit.Level.Value = 1;

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
        slot.BindUnitToSlot(controller);
    }
}
