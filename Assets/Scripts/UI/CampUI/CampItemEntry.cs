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

    public void AssignItem(InventoryEntry inventoryEntry)
    {
        InventoryEntry = inventoryEntry;
        ItemName.text = InventoryEntry.Item.ItemName;
        if (inventoryEntry.Item.Stackable)
        {
            ItemName.text = InventoryEntry.Item.ItemName + "(x" + inventoryEntry.Quantity + ")";
        }
        if (InventoryEntry.Item is EquipmentItemSO equipmentItemSO)
        {
            MainButton.enabled = true;
            MainButton.onClick.AddListener(() =>
            {
                CampManager.instance.TryEquipItem(this);
                CampManager.instance.CampItemEntries.Remove(this);
                Destroy(gameObject);
            });
        }
    }
}
