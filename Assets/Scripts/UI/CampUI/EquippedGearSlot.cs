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


    void Awake()
    {
        startingName = EquipmentName.text;
        button.onClick.AddListener(() =>
        {
            Unequip();
        });
    }

    public void EquipItemToSlot(InventoryEntry item)
    {
        if (ItemEquipped)
            Unequip(false);

        EquippedItem = item;
        EquipmentName.text = item.Item.ItemName;
        ItemEquipped = true;

        UnitController controller = CampManager.instance.TrackedUnitController;
        EquipmentItemSO equipmentItemSO = item.Item as EquipmentItemSO;
        foreach (var mod in equipmentItemSO.statModifiers)
        {
            controller.StatModifiers.Add(new StatModifier
            {
                stat = mod.stat,
                value = mod.value,
                sourceId = item.id,
            });
        }
        controller.CachedStatsDirty = true;

        controller.GetComponent<PlayerDataController>().EquippedItems.Add(item);

        CampManager.instance.PopulatePlayerStatsPanel();
        CampManager.instance.PopulateItemList();
    }

    public void Unequip(bool refreshUI = true)
    {
        if (!ItemEquipped)
            return;

        UnitController controller = CampManager.instance.TrackedUnitController;
        EquipmentItemSO equipmentItemSO = EquippedItem.Item as EquipmentItemSO;
        //Remove all stat modifiers from the controller that have this id

        controller.StatModifiers.RemoveAll(t => t.sourceId == EquippedItem.id);
        controller.CachedStatsDirty = true;

        controller.GetComponent<PlayerDataController>().EquippedItems.Remove(EquippedItem);

        EquippedItem = null;
        EquipmentName.text = startingName;
        ItemEquipped = false;

        if (refreshUI)
        {
            CampManager.instance.PopulatePlayerStatsPanel();
            CampManager.instance.PopulateItemList();
        }
    }
}
