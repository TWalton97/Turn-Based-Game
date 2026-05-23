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

    public void SetupAbilityButtons(List<BaseAbility> abilities)
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            abilityButtonControllers[i].EnableButton(abilities[i]);
        }
    }
}
