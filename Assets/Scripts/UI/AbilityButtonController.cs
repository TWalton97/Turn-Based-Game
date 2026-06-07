using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AbilityButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CombatMenuController combatMenuController;
    public BaseAbility Ability;
    public RuntimeAbilityInstance AbilityInstance;
    public ICombatActionSource CombatAction;
    public TextMeshProUGUI AbilityName;
    public TextMeshProUGUI CannotUseText;

    public Image image;

    public Color CanUseColor;
    public Color CannotUseColor;

    private void Awake()
    {
        combatMenuController = GetComponentInParent<CombatMenuController>();
    }

    public void EnableButton(UnitController controller, RuntimeAbilityInstance abilityInstance = null, BaseAbility baseAbility = null)
    {
        if (abilityInstance != null)
        {
            Ability = abilityInstance.Ability;
            AbilityInstance = abilityInstance;
            AbilityName.text = abilityInstance.Ability.AbilityName;

            if (!abilityInstance.CanUse(controller))
            {
                image.color = CannotUseColor;
                CannotUseText.enabled = true;
                if (abilityInstance.RemainingCooldownTurns > 0)
                {
                    CannotUseText.text = $"{abilityInstance.RemainingCooldownTurns} turns";
                }
                else
                {
                    CannotUseText.text = $"Not enough mana";
                }
            }
            else
            {
                image.color = CanUseColor;
                CannotUseText.enabled = false;
            }
        }
        else
        {
            Ability = baseAbility;
            AbilityName.text = baseAbility.AbilityName;
        }

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
        if (!AbilityInstance.CanUse(controller))
            return;

        if (Ability.TargetType == TargetType.Self || Ability.TargetType == TargetType.AllUnits)
        {
            CombatManager.instance.RequestCombatActionServerRpc(controller.NetworkObjectId, controller.GetAbilityIndex(Ability), controller.NetworkObjectId);
            combatMenuController.CloseAllMenus();
            return;
        }

        combatMenuController.ActivateTargetSelectionPanel(Ability);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.instance.EnableTooltipAtPosition(new TooltipData(Ability.AbilityName, Ability.ManaCost.ToString() + " Mana", Ability.AbilityDescription, "Cooldown: " + Ability.Cooldown), eventData.position + new Vector2(150, 0), this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.DisableTooltipIfSource?.Invoke(this);
    }
}
