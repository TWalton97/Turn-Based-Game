using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Collections;

public class PlayerDataController : NetworkBehaviour
{
    public UnitController UnitController;
    public NetworkVariable<int> CurrentExp;
    public NetworkVariable<int> AvailableStatPoints;
    public NetworkVariable<int> Gold;
    public NetworkList<InventoryEntry> InventoryItems;

    public List<AbilityUnlock> PendingAbilityUnlocks;

    public List<BaseAbility> UnchosenAbilities;

    void Awake()
    {
        UnitController = GetComponent<UnitController>();
        AssignStatsFromClassPreset();

        InventoryItems = new();
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
        if (IsOwner && newValue > 1)
        {
            CombatLogController.instance.AddLevelUp();
        }
        CheckAbilityUnlocks();
    }

    private void CheckAbilityUnlocks()
    {
        List<AbilityUnlock> abilitiesToUnlock = UnitController.UnitData.AbilityUnlocks.Where(t => t.LevelToUnlock == UnitController.Level.Value).ToList();
        foreach (AbilityUnlock abilityUnlock in abilitiesToUnlock)
        {
            if (abilityUnlock.AbilityUnlockType == AbilityUnlockType.AutoGrant && abilityUnlock.AbilityToUnlock.Count > 0)
            {
                UnitController.UnlockAbilityServerRpc(abilityUnlock.AbilityToUnlock[0].AbilityName);
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

    public void ServerAddItemToInventory(ItemSO item)
    {
        if (!IsServer)
            return;


        bool foundStack = false;
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].itemName == item.ItemName && InventoryItems[i].stackable)
            {
                InventoryEntry inventoryEntry = InventoryItems[i];
                inventoryEntry.quantity++;
                InventoryItems[i] = inventoryEntry;
                foundStack = true;
                break;
            }
        }

        if (!foundStack)
        {
            InventoryEntry inventoryEntry = new();
            inventoryEntry.itemName = item.ItemName;
            inventoryEntry.instanceId = Guid.NewGuid().ToString();
            inventoryEntry.quantity = 1;
            inventoryEntry.stackable = item.Stackable;
            inventoryEntry.equipped = false;
            InventoryItems.Add(inventoryEntry);
        }
    }

    public void RemoveItemFromInventoryById(FixedString64Bytes itemId)
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].instanceId == itemId)
            {
                InventoryEntry inventoryEntry = InventoryItems[i];
                inventoryEntry.quantity--;

                if (inventoryEntry.quantity <= 0)
                {
                    InventoryItems.RemoveAt(i);
                }
                else
                {
                    InventoryItems[i] = inventoryEntry;
                }
                break;
            }
        }
    }

    public void RemoveItemFromInventoryByName(string itemName)
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].itemName == itemName)
            {
                InventoryEntry inventoryEntry = InventoryItems[i];
                inventoryEntry.quantity--;

                if (inventoryEntry.quantity <= 0)
                {
                    InventoryItems.RemoveAt(i);
                }
                else
                {
                    InventoryItems[i] = inventoryEntry;
                }
                break;
            }
        }
    }

    public InventoryEntry FindInventoryEntryByItem(ItemSO item)
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].itemName == item.ItemName)
            {
                return InventoryItems[i];
            }
        }
        return default;
    }

    public InventoryEntry FindInventoryEntryByID(FixedString64Bytes id)
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].instanceId == id)
            {
                return InventoryItems[i];
            }
        }
        return default;
    }

    public bool IsItemEquipped(FixedString64Bytes id)
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].instanceId == id)
            {
                return InventoryItems[i].equipped;
            }
        }

        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryCraftItemServerRpc(FixedString64Bytes recipeOutputName)
    {
        RecipeSO recipeSO = RecipeDatabase.GetRecipeByName(recipeOutputName.ToString());
        TryRemoveItemsForRecipe(recipeSO);
        ServerAddItemToInventory(recipeSO.CraftingOutput);
    }

    public bool HasItemsForRecipe(RecipeSO recipe)
    {
        foreach (RecipeIngredient recipeIngredient in recipe.CraftingIngredients)
        {
            int total = 0;

            for (int i = 0; i < InventoryItems.Count; i++)
            {
                if (InventoryItems[i].itemName.ToString() == recipeIngredient.item.ItemName)
                {
                    total += InventoryItems[i].quantity;
                }
            }

            if (total < recipeIngredient.quantity)
                return false;
        }
        return true;
    }

    public void TryRemoveItemsForRecipe(RecipeSO recipe)
    {
        if (!IsServer)
            return;

        foreach (RecipeIngredient ingredient in recipe.CraftingIngredients)
        {
            int remainingToRemove = ingredient.quantity;

            for (int i = 0; i < InventoryItems.Count; i++)
            {
                if (InventoryItems[i].itemName.ToString() != ingredient.item.ItemName)
                    continue;

                InventoryEntry entry = InventoryItems[i];

                int remove = Mathf.Min(entry.quantity, remainingToRemove);
                entry.quantity -= remove;
                remainingToRemove -= remove;

                if (entry.quantity <= 0)
                    InventoryItems.RemoveAt(i);
                else
                    InventoryItems[i] = entry;

                if (remainingToRemove <= 0)
                    break;
            }
        }
    }

    public void ApplyEquipmentStats(EquipmentItemSO equipmentItemSO, InventoryEntry entry)
    {
        if (!IsServer)
            return;

        foreach (var mod in equipmentItemSO.statModifiers)
        {
            UnitController.StatModifiers.Add(new StatModifier
            {
                stat = mod.stat,
                value = mod.value,
                sourceId = entry.instanceId,
            });
        }
        UnitController.CachedStatsDirty = true;
        UnitController.RecalculateAllStats();
    }

    public void RemoveEquipmentStats(InventoryEntry entry)
    {
        if (!IsServer)
            return;

        for (int i = UnitController.StatModifiers.Count - 1; i >= 0; i--)
        {
            if (UnitController.StatModifiers[i].sourceId == entry.instanceId)
            {
                UnitController.StatModifiers.RemoveAt(i);
            }
        }

        UnitController.CachedStatsDirty = true;
        UnitController.RecalculateAllStats();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryEquipItemServerRpc(FixedString64Bytes instanceId)
    {
        //Checking to make sure we're equipping a real item
        EquipmentItemSO item = null;
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].instanceId == instanceId)
            {
                item = ItemDatabase.GetItemByName(InventoryItems[i].itemName.ToString()) as EquipmentItemSO;
                break;
            }
        }

        if (item == null)
        {
            Debug.LogWarning("Could not find item to equip");
            return;
        }

        //Checking if there's an equipped item of the same type and removing it
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].equipped)
            {
                EquipmentItemSO equipmentItem = ItemDatabase.GetItemByName(InventoryItems[i].itemName.ToString()) as EquipmentItemSO;
                if (equipmentItem.EquipmentSlot == item.EquipmentSlot)
                {
                    InventoryEntry entryCopy = InventoryItems[i];
                    entryCopy.equipped = false;
                    InventoryItems[i] = entryCopy;
                    RemoveEquipmentStats(entryCopy);
                    break;
                }
            }
        }

        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].instanceId == instanceId)
            {
                InventoryEntry entryCopy = InventoryItems[i];
                entryCopy.equipped = true;
                InventoryItems[i] = entryCopy;
                ApplyEquipmentStats(item, entryCopy);
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryUnequipItemServerRpc(FixedString64Bytes itemName)
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].equipped)
            {
                if (InventoryItems[i].itemName == itemName)
                {
                    InventoryEntry entryCopy = InventoryItems[i];
                    entryCopy.equipped = false;
                    InventoryItems[i] = entryCopy;
                    RemoveEquipmentStats(entryCopy);
                    break;
                }
            }
        }
    }
}

public struct InventoryEntry : INetworkSerializable, IEquatable<InventoryEntry>
{
    public FixedString64Bytes itemName;
    public FixedString64Bytes instanceId;
    public int quantity;
    public bool stackable;
    public bool equipped;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out itemName);
            reader.ReadValueSafe(out instanceId);
            reader.ReadValueSafe(out quantity);
            reader.ReadValueSafe(out stackable);
            reader.ReadValueSafe(out equipped);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(itemName);
            writer.WriteValueSafe(instanceId);
            writer.WriteValueSafe(quantity);
            writer.WriteValueSafe(stackable);
            writer.WriteValueSafe(equipped);
        }
    }

    public bool Equals(InventoryEntry other)
    {
        return itemName == other.itemName && instanceId == other.instanceId && quantity == other.quantity && stackable == other.stackable && equipped == other.equipped;
    }
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