using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CampItemEntry : MonoBehaviour
{
    public InventoryEntry InventoryEntry;
    public TextMeshProUGUI ItemName;

    public Button MainButton;
    public Button InspectButton;
    public Button TransferButton;

    private ItemSO itemSO;

    public void AssignItem(InventoryEntry inventoryEntry)
    {
        InventoryEntry = inventoryEntry;
        itemSO = ItemDatabase.GetItemByName(inventoryEntry.itemName.ToString());
        ItemName.text = itemSO.ItemName;
        if (itemSO.Stackable)
        {
            ItemName.text = itemSO.ItemName + "(x" + inventoryEntry.quantity + ")";
        }
        if (itemSO is EquipmentItemSO equipmentItemSO)
        {
            MainButton.enabled = true;
            MainButton.onClick.AddListener(() =>
            {
                CampManager.instance.TryEquipItem(this);
            });
        }
    }

    public string GenerateItemDescription()
    {
        string description = "";
        switch (itemSO)
        {
            case BattleItemSO:
                description = itemSO.ItemInformation;
                description += "\n" + "\n" + itemSO.GoldValue + " gold";
                break;
            case EquipmentItemSO:
                description = itemSO.ItemInformation;
                description += "\n" + "\n" + itemSO.GoldValue + " gold";
                break;
            case ResourceItemSO:
                description = itemSO.GoldValue + " gold";
                break;
        }

        return description;
    }
}
