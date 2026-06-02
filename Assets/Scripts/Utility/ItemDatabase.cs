using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ItemDatabase
{
    public static Dictionary<string, ItemSO> Items = new();

    public static ItemSO GetItemByName(string itemName)
    {
        return Items[itemName];
    }

    public static void RegisterItem(ItemSO itemSO)
    {
        if (Items.ContainsValue(itemSO))
            return;

        Items.Add(itemSO.ItemName, itemSO);
    }
}
