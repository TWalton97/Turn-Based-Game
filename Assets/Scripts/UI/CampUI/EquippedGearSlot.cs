using System.Collections;
using System.Collections.Generic;
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
            if (controller.statSetters.TryGetValue(mod.attribute, out var apply))
            {
                apply(mod.value);
            }
        }

        controller.GetComponent<PlayerDataController>().EquippedItems.Add(item);
        controller.RecalculateCombatStats();

        CampManager.instance.PopulatePlayerStatsPanel();
        CampManager.instance.PopulateItemList();
    }

    public void Unequip(bool refreshUI = true)
    {
        if (!ItemEquipped)
            return;

        UnitController controller = CampManager.instance.TrackedUnitController;
        EquipmentItemSO equipmentItemSO = EquippedItem.Item as EquipmentItemSO;
        foreach (var mod in equipmentItemSO.statModifiers)
        {
            if (controller.statSetters.TryGetValue(mod.attribute, out var apply))
            {
                apply(-mod.value);
            }
        }

        controller.GetComponent<PlayerDataController>().EquippedItems.Remove(EquippedItem);
        controller.RecalculateCombatStats();

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
