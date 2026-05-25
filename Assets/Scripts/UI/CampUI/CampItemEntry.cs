using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CampItemEntry : MonoBehaviour
{
    public ItemSO Item;
    public TextMeshProUGUI ItemName;

    public Button MainButton;
    public Button InspectButton;

    public void AssignItem(ItemSO item)
    {
        Item = item;
        ItemName.text = Item.ItemName;
        if (Item is EquipmentItemSO equipmentItemSO)
        {
            MainButton.enabled = true;
            MainButton.onClick.AddListener(() =>
            {
                CampManager.instance.TryEquipItem(this, equipmentItemSO);
                CampManager.instance.CampItemEntries.Remove(this);
                Destroy(gameObject);
            });
        }
    }
}
