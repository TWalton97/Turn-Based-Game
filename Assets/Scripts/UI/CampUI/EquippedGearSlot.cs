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
        if (ItemEquipped != default)
            Unequip(false);

        ItemSO itemSO = ItemDatabase.GetItemByName(item.itemName.ToString());
        EquippedItem = item;
        EquipmentName.text = itemSO.ItemName;
        ItemEquipped = true;

        UnitController controller = CampManager.instance.TrackedUnitController;
        EquipmentItemSO equipmentItemSO = itemSO as EquipmentItemSO;
        foreach (var mod in equipmentItemSO.statModifiers)
        {
            controller.StatModifiers.Add(new StatModifier
            {
                stat = mod.stat,
                value = mod.value,
                sourceId = item.instanceId.ToString(),
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

        ItemSO itemSO = ItemDatabase.GetItemByName(EquippedItem.itemName.ToString());
        UnitController controller = CampManager.instance.TrackedUnitController;
        EquipmentItemSO equipmentItemSO = itemSO as EquipmentItemSO;
        //Remove all stat modifiers from the controller that have this id

        controller.StatModifiers.RemoveAll(t => t.sourceId == EquippedItem.instanceId);
        controller.CachedStatsDirty = true;

        controller.GetComponent<PlayerDataController>().EquippedItems.Remove(EquippedItem);

        EquippedItem = default;
        EquipmentName.text = startingName;
        ItemEquipped = false;

        if (refreshUI)
        {
            CampManager.instance.PopulatePlayerStatsPanel();
            CampManager.instance.PopulateItemList();
        }
    }
}
