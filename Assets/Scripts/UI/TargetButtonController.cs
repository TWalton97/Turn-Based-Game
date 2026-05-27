using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TargetButtonController : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CombatMenuController combatMenuController;
    public TextMeshProUGUI UnitName;
    public Image HealthBarFill;
    private BaseAbility ability;
    private UnitController target;

    private List<UnitController> unitControllers = new();

    private void Awake()
    {
        combatMenuController = GetComponentInParent<CombatMenuController>();
    }

    public void EnableButton(UnitController unit, BaseAbility ability)
    {
        UnitName.text = unit.UnitName;
        HealthBarFill.fillAmount = (float)unit.CurrentHealth.Value / unit.MaxHealth;
        this.ability = ability;
        this.target = unit;
        unitControllers = ReturnTargetControllers();
        gameObject.SetActive(true);
    }

    public void DisableButton()
    {
        gameObject.SetActive(false);
    }

    public void ActivateButton()
    {
        Debug.Log("Using ability " + ability.AbilityName);
        //combatMenuController.CurrentUnitController.TryUseAbility(combatMenuController.CurrentUnitController.GetAbilityIndex(ability), target.NetworkObjectId);
        combatMenuController.CurrentUnitController.RequestUseAbilityServerRpc(combatMenuController.CurrentUnitController.GetAbilityIndex(ability), target.NetworkObjectId);

        //Instead of using this ability, it should tell the server to use this ability...

        combatMenuController.CloseAllMenus();
    }

    private List<UnitController> ReturnTargetControllers()
    {
        unitControllers.Clear();

        if (ability.TargetType == TargetType.SingleUnit)
        {
            unitControllers.Add(target);
        }
        else
        {
            switch (ability.TeamTargeting)
            {
                case Team.Enemy:
                    foreach (UnitController controller in BattleManager.instance.EnemyUnits)
                    {
                        unitControllers.Add(controller);
                    }
                    break;

                case Team.Ally:
                    foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
                    {
                        unitControllers.Add(controller);
                    }
                    break;
            }
        }
        return unitControllers;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (unitControllers.Count == 0)
            return;

        foreach (UnitController controller in unitControllers)
        {
            controller.EnableHighlight();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (unitControllers.Count == 0)
            return;

        foreach (UnitController controller in unitControllers)
        {
            controller.DisableHighlight();
        }
    }
}
