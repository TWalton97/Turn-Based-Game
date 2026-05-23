using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TargetButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CombatMenuController combatMenuController;
    public TextMeshProUGUI UnitName;
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
        combatMenuController.CurrentUnitController.UpdateMana(ability.ManaCost);
        StartCoroutine(PlayAttackAnimation());

        combatMenuController.CloseAllMenus();
    }

    private IEnumerator PlayAttackAnimation()
    {
        Vector3 startPos = combatMenuController.CurrentUnitController.characterModel.transform.position;
        Vector3 targetPos = target.characterModel.transform.position + (target.characterModel.transform.forward * 2f);
        float elapsedTime = 0f;

        while (elapsedTime < 0.2f)
        {
            elapsedTime += Time.deltaTime;
            combatMenuController.CurrentUnitController.characterModel.transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / 0.2f);
            yield return null;
        }

        yield return new WaitForSeconds(0.8f);
        foreach (UnitController controller in unitControllers)
            controller.UpdateHealth(ability.DamageAmount);

        elapsedTime = 0f;
        while (elapsedTime < 0.2f)
        {
            elapsedTime += Time.deltaTime;
            combatMenuController.CurrentUnitController.characterModel.transform.position = Vector3.Lerp(targetPos, startPos, elapsedTime / 0.2f);
            yield return null;
        }

        combatMenuController.CurrentUnitController.characterModel.transform.position = startPos;
        combatMenuController.CurrentUnitController.IsActiveTurn = false;
        BattleManager.OnTurnEnded?.Invoke();
        yield return null;
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
