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
    public void RequestMoveItemToStashServerRpc(ulong unitId, FixedString64Bytes itemInstanceId)
    {
        UnitController controller = NetworkUtilities.GetUnitControllerById(unitId);
        PlayerDataController dataController = controller.GetComponent<PlayerDataController>();
        InventoryEntry inventoryEntry = dataController.FindInventoryEntryByID(itemInstanceId);

        if (inventoryEntry.quantity == 0)
        {
            Debug.LogWarning($"Item does not exist in unit's inventory");
            return;
        }

        ItemSO item = ItemDatabase.GetItemByName(inventoryEntry.itemName.ToString());

        bool foundStack = false;

        for (int i = 0; i < stashEntries.Count; i++)
        {
            if (stashEntries[i].itemName == item.ItemName && stashEntries[i].stackable)
            {
                StashEntry stashEntry = stashEntries[i];
                stashEntry.quantity++;
                stashEntries[i] = stashEntry;

                foundStack = true;
                break;
            }
        }

        if (!foundStack)
        {
            StashEntry stashEntry = new();
            stashEntry.itemName = item.ItemName;
            stashEntry.instanceId = inventoryEntry.instanceId;
            stashEntry.stackable = item.Stackable;
            stashEntry.quantity = 1;

            stashEntries.Add(stashEntry);
        }

        dataController.RemoveItemFromInventoryById(itemInstanceId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveItemToInventoryServerRpc(ulong unitId, FixedString64Bytes itemInstanceId)
    {
        UnitController controller = NetworkUtilities.GetUnitControllerById(unitId);
        PlayerDataController dataController = controller.GetComponent<PlayerDataController>();

        for (int i = 0; i < stashEntries.Count; i++)
        {
            if (stashEntries[i].instanceId == itemInstanceId)
            {
                ItemSO item = ItemDatabase.GetItemByName(stashEntries[i].itemName.ToString());
                dataController.ServerAddItemToInventory(item);

                StashEntry stashEntry = stashEntries[i];
                stashEntry.quantity--;
                if (stashEntry.quantity <= 0)
                {
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
