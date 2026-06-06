using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class BattleManager : NetworkBehaviour
{
    public static BattleManager instance;

    //Unit and slot references
    public List<BattleSlot> BattleSlots;
    public List<UnitController> AllUnits;
    public List<UnitController> FriendlyUnits;
    public List<UnitController> EnemyUnits;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void RegisterUnit(UnitController controller)
    {
        AllUnits.Add(controller);

        if (controller.UnitTeam == Team.Ally)
            FriendlyUnits.Add(controller);

        if (controller.UnitTeam == Team.Enemy)
            EnemyUnits.Add(controller);
    }

    public void UnregisterUnit(UnitController controller)
    {
        if (AllUnits.Contains(controller))
            AllUnits.Remove(controller);

        if (FriendlyUnits.Contains(controller))
            FriendlyUnits.Remove(controller);

        if (EnemyUnits.Contains(controller))
            EnemyUnits.Remove(controller);
    }

    public void RemoveAllEnemies()
    {
        foreach (UnitController controller in EnemyUnits)
        {
            AllUnits.Remove(controller);
        }
        EnemyUnits.Clear();
    }

    public BattleSlot ReturnEmptyBattleSlotOfType(Team team)
    {
        for (int i = 0; i < BattleSlots.Count; i++)
        {
            if (BattleSlots[i].Team == team && BattleSlots[i].UnitController == null)
                return BattleSlots[i];
        }
        Debug.Log("No battle slot available");
        return null;
    }


    public void DistributeExp(int amount)
    {
        if (!IsServer)
            return;

        foreach (UnitController controller in FriendlyUnits)
        {
            if (controller.ServerIsAlive.Value)
            {
                if (controller.TryGetComponent(out PlayerDataController dataController))
                {
                    dataController.ServerAddExp(amount);
                    DistributeExpClientRpc(controller.OwnerClientId, amount);
                }
            }
        }
    }

    [ClientRpc]
    public void DistributeExpClientRpc(ulong targetClientId, int amount)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        CombatLogController.instance.AddExpToCombatLog(amount);
    }

    public void DistributeItemsToPlayer(ItemSO item)
    {
        if (!IsServer)
            return;

        foreach (UnitController controller in FriendlyUnits)
        {
            if (controller.ServerIsAlive.Value)
            {
                if (controller.TryGetComponent(out PlayerDataController dataController))
                {
                    for (int i = 0; i < UnityEngine.Random.Range(1, 3); i++)
                    {
                        dataController.ServerAddItemToInventory(item);
                        DistributeItemsClientRpc(controller.OwnerClientId, item.ItemName);
                    }
                }
            }
        }
    }

    [ClientRpc]
    public void DistributeItemsClientRpc(ulong targetClientId, FixedString64Bytes itemName)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        ItemSO item = ItemDatabase.GetItemByName(itemName.ToString());
        CombatLogController.instance.AddItemToCombatlog(item, 1);
    }
}
