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
    public EquipmentItemSO EquippedItem;

    void Awake()
    {
        startingName = EquipmentName.text;
    }

    public void EquipItemToSlot(EquipmentItemSO item)
    {
        EquippedItem = item;
        EquipmentName.text = item.ItemName;

        UnitController controller = CampManager.instance.TrackedUnitController;
        foreach (var mod in item.statModifiers)
        {
            if (controller.statSetters.TryGetValue(mod.attribute, out var apply))
            {
                apply(mod.value);
            }
        }

        controller.GetComponent<PlayerDataController>().EquippedItems.Add(item);

        button.onClick.AddListener(() =>
        {
            Unequip();
        });
    }

    public void Unequip()
    {
        if (EquippedItem == null)
            return;

        UnitController controller = CampManager.instance.TrackedUnitController;
        foreach (var mod in EquippedItem.statModifiers)
        {
            if (controller.statSetters.TryGetValue(mod.attribute, out var apply))
            {
                apply(-mod.value);
            }
        }

        controller.GetComponent<PlayerDataController>().EquippedItems.Remove(EquippedItem);
        
        CampManager.instance.PopulatePlayerStatsPanel();
        CampManager.instance.PopulateItemList();

        EquippedItem = null;
        EquipmentName.text = startingName;
    }
}
