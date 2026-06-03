using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Battle Item")]
public class BattleItemSO : ItemSO, ICombatActionSource
{
    public BaseAbility ability;

    public bool CanUse(UnitController controller)
    {
        PlayerDataController player = controller.GetComponent<PlayerDataController>();
        InventoryEntry entry = player.FindInventoryEntryByItem(this);
        return entry.quantity > 0;
    }

    public void ConsumeCost(UnitController controller)
    {
        PlayerDataController player = controller.GetComponent<PlayerDataController>();
        player.RemoveItemFromInventoryByName(this.ItemName);
    }
}
