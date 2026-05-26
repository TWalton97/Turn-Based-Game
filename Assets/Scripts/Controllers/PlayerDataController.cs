using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PlayerDataController : MonoBehaviour
{
    //Stores information specific to the player
    //Inventory items
    //Level and exp
    //Stat points
    //Gold

    private UnitController UnitController;
    public PlayerStats PlayerStats;
    public List<InventoryEntry> InventoryItems;
    public List<InventoryEntry> EquippedItems;

    public Action OnInventoryUpdated;

    void Awake()
    {
        UnitController = GetComponent<UnitController>();
        AssignStatsFromClassPreset();
    }

    private void AssignStatsFromClassPreset()
    {
        PlayerStats.Level = 1;
        PlayerStats.CurrentExp = 0;
        PlayerStats.AvailableStatPoints = 0;
        PlayerStats.Gold = 0;
    }

    public void AddExp(int amount)
    {
        PlayerStats.CurrentExp += amount;
        if (PlayerStats.CurrentExp >= ExperienceValues.ExpToNextLevel[PlayerStats.Level])
        {
            PlayerStats.CurrentExp -= ExperienceValues.ExpToNextLevel[PlayerStats.Level];
            PlayerStats.Level += 1;
            PlayerStats.AvailableStatPoints += 3;
            AddExp(0);
        }
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

[Serializable]
public class PlayerStats
{
    public int Level;
    public int CurrentExp;
    public int AvailableStatPoints;
    public int Gold;
}

public static class ExperienceValues
{
    public static List<int> ExpToNextLevel = new()
    {
        0, 15, 25, 45, 70, 100,
    };
}