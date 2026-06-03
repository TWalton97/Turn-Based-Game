using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPanelController : PanelController
{
    public ItemButtonController[] abilityButtonControllers;

    public override void Awake()
    {
        base.Awake();
        abilityButtonControllers = GetComponentsInChildren<ItemButtonController>();
        foreach (ItemButtonController abilityButtonController in abilityButtonControllers)
        {
            abilityButtonController.DisableButton();
        }
    }

    public void SetupItemButtons(UnitController controller)
    {
        if (controller.enemyController != null || !controller.IsOwner)
            return;

        PlayerDataController dataController = controller.GetComponent<PlayerDataController>();

        foreach (AbilityButtonController abilityButtonController in abilityButtonControllers)
        {
            abilityButtonController.DisableButton();
        }

        int i = 0;
        foreach (InventoryEntry entry in dataController.InventoryItems)
        {
            ItemSO itemSO = ItemDatabase.GetItemByName(entry.itemName.ToString());
            if (itemSO is BattleItemSO battleItem)
            {
                abilityButtonControllers[i].BattleItem = battleItem;
                abilityButtonControllers[i].EnableButton(controller, null, battleItem.ability);
                i++;
            }
        }
    }
}
