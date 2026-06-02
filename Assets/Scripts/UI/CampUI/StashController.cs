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

        Debug.Log($"Trying to find inventory entry with id {itemInstanceId}");

        if (inventoryEntry == null)
        {
            Debug.LogWarning($"Item does not exist in unit's inventory");
            return;
        }

        StashEntry stashEntry = new();
        stashEntry.itemName = inventoryEntry.Item.ItemName;
        stashEntry.instanceId = inventoryEntry.id;
        stashEntries.Add(stashEntry);

        dataController.RemoveItemFromInventory(itemInstanceId);
        RemoveItemFromTargetInventoryClientRpc(unitId, itemInstanceId);
    }

    [ClientRpc]
    private void RemoveItemFromTargetInventoryClientRpc(ulong unitId, string itemId)
    {
        if (IsServer)
            return;

        UnitController controller = NetworkUtilities.GetUnitControllerById(unitId);
        PlayerDataController dataController = controller.GetComponent<PlayerDataController>();

        dataController.RemoveItemFromInventory(itemId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveItemToInventoryServerRpc(ulong unitId, string itemInstanceId)
    {
        UnitController controller = NetworkUtilities.GetUnitControllerById(unitId);
        PlayerDataController dataController = controller.GetComponent<PlayerDataController>();

        StashEntry entry;
        if (TryFindStashEntryById(itemInstanceId, out entry))
        {
            ItemSO item = ItemDatabase.GetItemByName(entry.itemName.ToString());
            dataController.ServerAddItemToInventory(item, itemInstanceId);
            MoveItemToTargetInventoryClientRpc(unitId, entry.itemName.ToString(), itemInstanceId);
            stashEntries.Remove(entry);
        }
        else
        {
            Debug.LogWarning($"Could not find stash entry with id {itemInstanceId}");
        }
    }

    [ClientRpc]
    public void MoveItemToTargetInventoryClientRpc(ulong unitId, string itemName, string itemInstanceId)
    {
        if (IsServer)
            return;

        UnitController controller = NetworkUtilities.GetUnitControllerById(unitId);
        PlayerDataController dataController = controller.GetComponent<PlayerDataController>();

        ItemSO item = ItemDatabase.GetItemByName(itemName.ToString());
        dataController.ServerAddItemToInventory(item, itemInstanceId);
    }

    private bool TryFindStashEntryById(string instanceId, out StashEntry entry)
    {
        FixedString64Bytes id = instanceId;

        for (int i = 0; i < stashEntries.Count; i++)
        {
            if (stashEntries[i].instanceId == id)
            {
                entry = stashEntries[i];
                return true;
            }
        }

        entry = default;
        return false;
    }
}

public struct StashEntry : INetworkSerializable, IEquatable<StashEntry>
{
    public FixedString64Bytes itemName;
    public FixedString64Bytes instanceId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out itemName);
            reader.ReadValueSafe(out instanceId);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(itemName);
            writer.WriteValueSafe(instanceId);
        }
    }

    public bool Equals(StashEntry other)
    {
        return itemName == other.itemName && instanceId == other.instanceId;
    }
}
