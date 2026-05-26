using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemButtonController : AbilityButtonController, IPointerEnterHandler, IPointerExitHandler
{
    public BattleItemSO BattleItem;
    public override void ActivateButton()
    {
        UnitController controller = TurnManager.instance.UnitControllerTurnOrder[TurnManager.instance.CurrentTurnIndex];
        if (!Ability.CanUse(controller))
            return;

        controller.GetComponent<PlayerDataController>().RemoveItemFromInventory(BattleItem);

        if (Ability.TargetType == TargetType.Self)
        {
            controller.TryUseAbility(Ability, controller);
            combatMenuController.ItemPanel.ActivatePanel();
            return;
        }

        combatMenuController.ActivateTargetSelectionPanel(Ability);
    }
}
