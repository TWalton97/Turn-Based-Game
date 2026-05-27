using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class AbilityButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CombatMenuController combatMenuController;
    public BaseAbility Ability;
    public ICombatActionSource CombatAction;
    public TextMeshProUGUI AbilityName;

    private void Awake()
    {
        combatMenuController = GetComponentInParent<CombatMenuController>();
    }

    public void EnableButton(BaseAbility ability)
    {
        Ability = ability;
        AbilityName.text = ability.AbilityName;
        gameObject.SetActive(true);
    }

    public void DisableButton()
    {
        Ability = null;
        gameObject.SetActive(false);
    }

    public virtual void ActivateButton()
    {
        UnitController controller = combatMenuController.CurrentUnitController;
        if (!Ability.CanUse(controller))
            return;

        if (Ability.TargetType == TargetType.Self)
        {
            controller.TryUseAbility(controller.GetAbilityIndex(Ability), controller.NetworkObjectId);
            combatMenuController.ItemPanel.ActivatePanel();
            return;
        }

        combatMenuController.ActivateTargetSelectionPanel(Ability);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.instance.EnableTooltipAtPosition(new TooltipData(Ability.AbilityName, Ability.ManaCost.ToString() + " Mana", Ability.AbilityDescription, "Cooldown: " + Ability.Cooldown), eventData.position + new Vector2(150, 0));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.instance.DisableTooltip();
    }
}
