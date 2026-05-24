using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightPanelController : PanelController
{
    public AbilityButtonController[] abilityButtonControllers;

    public override void Awake()
    {
        base.Awake();
        abilityButtonControllers = GetComponentsInChildren<AbilityButtonController>();
        foreach (AbilityButtonController abilityButtonController in abilityButtonControllers)
        {
            abilityButtonController.DisableButton();
        }
    }

    public void SetupAbilityButtons(UnitController controller)
    {
        if (controller.enemyController != null)
            return;

        foreach (AbilityButtonController abilityButtonController in abilityButtonControllers)
        {
            abilityButtonController.DisableButton();
        }

        for (int i = 0; i < controller.Abilities.Count; i++)
        {
            abilityButtonControllers[i].EnableButton(controller.Abilities[i]);
        }
    }
}
