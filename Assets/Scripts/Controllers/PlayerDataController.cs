using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Netcode;

public class PlayerDataController : NetworkBehaviour
{
    public UnitController UnitController;
    public NetworkVariable<int> CurrentExp;
    public NetworkVariable<int> AvailableStatPoints;
    public NetworkVariable<int> Gold;
    public List<InventoryEntry> InventoryItems;
    public List<InventoryEntry> EquippedItems;

    public Action OnInventoryUpdated;

    public List<AbilityUnlock> PendingAbilityUnlocks;

    void Awake()
    {
        UnitController = GetComponent<UnitController>();
        AssignStatsFromClassPreset();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        UnitController.Level.OnValueChanged += OnLevelUpChanges;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        UnitController.Level.OnValueChanged -= OnLevelUpChanges;
    }

    private void AssignStatsFromClassPreset()
    {
        CurrentExp.Value = 0;
        AvailableStatPoints.Value = 0;
        Gold.Value = 0;
    }

    public void ServerAddExp(int amount)
    {
        CurrentExp.Value += amount;
        if (CurrentExp.Value >= ExperienceValues.ExpToNextLevel[UnitController.Level.Value])
        {
            CurrentExp.Value -= ExperienceValues.ExpToNextLevel[UnitController.Level.Value];
            UnitController.Level.Value += 1;
            AvailableStatPoints.Value += 3;
            ServerAddExp(0);
        }
    }

    public void OnLevelUpChanges(int oldValue, int newValue)
    {
        CheckAbilityUnlocks();
    }

    private void CheckAbilityUnlocks()
    {
        List<AbilityUnlock> abilitiesToUnlock = UnitController.UnitData.AbilityUnlocks.Where(t => t.LevelToUnlock == UnitController.Level.Value).ToList();
        foreach (AbilityUnlock abilityUnlock in abilitiesToUnlock)
        {
            if (abilityUnlock.AbilityUnlockType == AbilityUnlockType.AutoGrant && abilityUnlock.AbilityToUnlock.Count > 0)
            {
                UnitController.UnlockAbility(abilityUnlock.AbilityToUnlock[0]);
            }
            else
            {
                PendingAbilityUnlocks.Add(abilityUnlock);
            }
        }
    }

    public void RemovePendingAbilityUnlock(AbilityUnlock pendingAbilityUnlock)
    {
        PendingAbilityUnlocks.Remove(pendingAbilityUnlock);
    }

    public void AddItemToInventory(ItemSO item)
    {
        InventoryEntry existingEntry = InventoryItems.Find(entry => entry.Item == item);

        if (item.Stackable && existingEntry != null)
        {
            existingEntry.Quantity += 1;
        }
        else
        {
            InventoryItems.Add(new InventoryEntry
            {
                Item = item,
                Quantity = 1,
                id = Guid.NewGuid().ToString()
            });
        }
        OnInventoryUpdated?.Invoke();
    }

    public void RemoveItemFromInventory(ItemSO item)
    {
        InventoryEntry existingEntry = InventoryItems.Find(entry => entry.Item == item);

        if (existingEntry != null)
        {
            existingEntry.Quantity -= 1;
        }

        if (existingEntry.Quantity <= 0)
        {
            InventoryItems.Remove(existingEntry);
        }

        OnInventoryUpdated?.Invoke();
    }

    public InventoryEntry FindInventoryEntryByItem(ItemSO item)
    {
        InventoryEntry entry = InventoryItems.Find(x => x.Item == item);
        return entry;
    }

    public InventoryEntry FindInventoryEntryByID(string id)
    {
        InventoryEntry entry = InventoryItems.Find(x => x.id == id);
        return null;
    }

    public bool IsItemEquipped(string id)
    {
        InventoryEntry entry = EquippedItems.Find(x => x.id == id);
        return entry != null;
    }

    public bool HasItemsForRecipe(RecipeSO recipe)
    {
        foreach (InventoryEntry entry in recipe.CraftingIngredients)
        {
            InventoryEntry existingEntry = InventoryItems.Find(e => e.Item == entry.Item);

            if (existingEntry == null || existingEntry.Quantity < entry.Quantity)
            {
                return false;
            }
        }
        return true;
    }

    public void TryRemoveItemsForRecipe(RecipeSO recipe)
    {
        foreach (InventoryEntry entry in recipe.CraftingIngredients)
        {
            InventoryEntry existingEntry = InventoryItems.Find(e => e.Item == entry.Item);

            if (existingEntry != null && existingEntry.Quantity >= entry.Quantity)
            {
                existingEntry.Quantity -= entry.Quantity;
                if (existingEntry.Quantity == 0)
                    InventoryItems.Remove(existingEntry);

                OnInventoryUpdated?.Invoke();
            }
        }
    }
}

[Serializable]
public class InventoryEntry
{
    public ItemSO Item;
    public int Quantity;

    public string id;
}

public static class ExperienceValues
{
    public static List<int> ExpToNextLevel = new()
    {
        0, 15, 25, 45, 70, 100,
    };
}

[Serializable]
public class AbilityUnlock
{
    public int LevelToUnlock;
    public AbilityUnlockType AbilityUnlockType;
    public List<BaseAbility> AbilityToUnlock;
}

[Serializable]
public enum AbilityUnlockType
{
    AutoGrant,
    Choice
}