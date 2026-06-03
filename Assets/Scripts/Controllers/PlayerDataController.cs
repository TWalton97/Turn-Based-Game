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
    public NetworkList<InventoryEntry> EquippedItems;

    public Action OnInventoryUpdated;

    public List<AbilityUnlock> PendingAbilityUnlocks;

    void Awake()
    {
        UnitController = GetComponent<UnitController>();
        AssignStatsFromClassPreset();

        InventoryItems = new();
        EquippedItems = new();
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

        OnInventoryUpdated?.Invoke();
    }

    public void RemoveItemFromInventoryById(FixedString64Bytes itemId)
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].instanceId == itemId)
            {
                Debug.Log($"Removing {InventoryItems[i].itemName} from inventory");
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

        OnInventoryUpdated?.Invoke();
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

        OnInventoryUpdated?.Invoke();
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

        Debug.LogWarning($"No item with id {id} found in inventory");
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryCraftItemServerRpc(FixedString64Bytes recipeOutputName)
    {
        RecipeSO recipeSO = RecipeDatabase.GetRecipeByName(recipeOutputName.ToString());
        TryRemoveItemsForRecipe(recipeSO);
        ServerAddItemToInventory(recipeSO.CraftingOutput);
    }

    //TODO: re-add recipes
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

    //TODO: re-add recipes
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

    public void SetEquipped(string id, bool equipped)
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].instanceId.ToString() == id)
            {
                InventoryEntry entry = InventoryItems[i];
                entry.equipped = equipped;
                InventoryItems[i] = entry;
                break;
            }
        }

        OnInventoryUpdated?.Invoke();
    }

    public void ApplyEquipmentStats(EquipmentItemSO equipmentItemSO, InventoryEntry entry)
    {
        foreach (var mod in equipmentItemSO.statModifiers)
        {
            UnitController.StatModifiers.Add(new StatModifier
            {
                stat = mod.stat,
                value = mod.value,
                sourceId = entry.instanceId.ToString(),
            });
        }
        UnitController.CachedStatsDirty = true;
        EquippedItems.Add(entry);
    }

    [ServerRpc(RequireOwnership = false)]
    public void EquipItemServerRpc(string itemInstanceId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        InventoryEntry entry = FindInventoryEntryByID(itemInstanceId);

        ItemSO item = ItemDatabase.GetItemByName(entry.itemName.ToString());
        EquipmentItemSO equipment = item as EquipmentItemSO;

        if (equipment == null)
            return;

        SetEquipped(itemInstanceId, true);

        ApplyEquipmentStats(equipment, entry);
    }

    [ClientRpc]
    public void UpdateEquippedItemUIClientRpc(string itemInstanceId, ulong senderId)
    {
        CampManager.instance.ConfirmEquipItem(itemInstanceId, senderId);
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