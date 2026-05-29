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
        UnitController controller = TurnManager.instance.UnitControllerTurnOrder[TurnManager.instance.ServerCurrentTurnIndex];
        if (!Ability.CanUse(controller))
            return;

        controller.GetComponent<PlayerDataController>().RemoveItemFromInventory(BattleItem);

        if (Ability.TargetType == TargetType.Self)
        {
            CombatManager.instance.RequestCombatActionServerRpc(controller.NetworkObjectId, controller.GetAbilityIndex(Ability), controller.NetworkObjectId);
            combatMenuController.ItemPanel.ActivatePanel();
            return;
        }

        combatMenuController.ActivateTargetSelectionPanel(Ability);
    }
}
