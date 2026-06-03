using System;
using System.Collections.Generic;
using System.Linq;
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

    public ItemSO item1;
    public ItemSO item2;

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

        DistributeItemsToPlayer(item1);
        DistributeItemsToPlayer(item2);
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
                }
            }
        }
    }

    public void DistributeItemsToPlayer(ItemSO item)
    {
        foreach (UnitController controller in FriendlyUnits)
        {
            if (controller.ServerIsAlive.Value)
            {
                if (controller.TryGetComponent(out PlayerDataController dataController))
                {
                    for (int i = 0; i < UnityEngine.Random.Range(1, 3); i++)
                    {
                        dataController.ServerAddItemToInventory(item);
                    }
                }
            }
        }
    }

}
