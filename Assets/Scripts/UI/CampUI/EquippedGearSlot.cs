using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquippedGearSlot : MonoBehaviour
{
    private string startingName;
    public TextMeshProUGUI EquipmentName;

    public Button button;

    public EquipmentSlot EquipmentSlot;
    public InventoryEntry EquippedItem;

    public bool ItemEquipped = false;

    private EquipmentItemSO equippedItem;


    void Awake()
    {
        startingName = EquipmentName.text;
        button.onClick.AddListener(() =>
        {
            Unequip();
        });
    }

    public void Reset()
    {
        EquipmentName.text = startingName;
        equippedItem = null;
    }

    public void EquipItemToSlot(EquipmentItemSO equipmentItemSO)
    {
        EquipmentName.text = equipmentItemSO.ItemName;
        equippedItem = equipmentItemSO;
    }

    public void Unequip()
    {
        CampManager.instance.TryUnequipItem(equippedItem);
    }
}
