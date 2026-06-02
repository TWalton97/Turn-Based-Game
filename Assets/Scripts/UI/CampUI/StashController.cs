using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class StashController : NetworkBehaviour
{
    public static StashController instance;

    public NetworkList<StashEntry> stashEntries;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        stashEntries = new();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveItemToStashServerRpc(ulong unitId, string itemInstanceId)
    {
        UnitController controller = NetworkUtilities.GetUnitControllerById(unitId);
        PlayerDataController dataController = controller.GetComponent<PlayerDataController>();
        InventoryEntry inventoryEntry = dataController.FindInventoryEntryByID(itemInstanceId);

        if (inventoryEntry == null)
        {
            Debug.LogWarning($"Item does not exist in unit's inventory");
            return;
        }

        bool foundStack = false;

        for (int i = 0; i < stashEntries.Count; i++)
        {
            if (stashEntries[i].itemName == inventoryEntry.Item.ItemName && stashEntries[i].stackable)
            {
                StashEntry stashEntry = stashEntries[i];
                stashEntry.quantity++;
                stashEntries[i] = stashEntry;
                Debug.Log($"Found stash entry for {inventoryEntry.Item.ItemName}, increasing quantity to {stashEntry.quantity}");

                foundStack = true;
                break;
            }
        }

        if (!foundStack)
        {
            StashEntry stashEntry = new();
            stashEntry.itemName = inventoryEntry.Item.ItemName;
            stashEntry.instanceId = inventoryEntry.id;
            stashEntry.stackable = inventoryEntry.Item.Stackable;
            stashEntry.quantity = 1;

            stashEntries.Add(stashEntry);

            Debug.Log($"No stash entry exists with item {inventoryEntry.Item.ItemName}, creating one");
        }

        dataController.RemoveItemFromInventory(itemInstanceId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveItemToInventoryServerRpc(ulong unitId, string itemInstanceId)
    {
        UnitController controller = NetworkUtilities.GetUnitControllerById(unitId);
        PlayerDataController dataController = controller.GetComponent<PlayerDataController>();

        for (int i = 0; i < stashEntries.Count; i++)
        {
            if (stashEntries[i].instanceId == itemInstanceId)
            {

                ItemSO item = ItemDatabase.GetItemByName(stashEntries[i].itemName.ToString());
                Debug.Log($"Found stash entry for {item.ItemName}, decreasing quantity to {stashEntries[i].quantity - 1}");
                dataController.ServerAddItemToInventory(item, itemInstanceId);

                StashEntry stashEntry = stashEntries[i];
                stashEntry.quantity--;
                if (stashEntry.quantity <= 0)
                {
                    Debug.Log($"Remaining quantity for {item.ItemName} is 0, removing stash entry");
                    stashEntries.RemoveAt(i);
                }
                else
                {
                    stashEntries[i] = stashEntry;
                }
                break;
            }
        }
    }
}

public struct StashEntry : INetworkSerializable, IEquatable<StashEntry>
{
    public FixedString64Bytes itemName;
    public FixedString64Bytes instanceId;
    public int quantity;
    public bool stackable;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out itemName);
            reader.ReadValueSafe(out instanceId);
            reader.ReadValueSafe(out quantity);
            reader.ReadValueSafe(out stackable);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(itemName);
            writer.WriteValueSafe(instanceId);
            writer.WriteValueSafe(quantity);
            writer.WriteValueSafe(stackable);
        }
    }

    public bool Equals(StashEntry other)
    {
        return itemName == other.itemName && instanceId == other.instanceId && quantity == other.quantity && stackable == other.stackable;
    }
}
