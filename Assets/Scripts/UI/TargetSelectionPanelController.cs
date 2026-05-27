using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSelectionPanelController : FightPanelController
{
    public TargetButtonController[] targetButtonControllers;

    public override void Awake()
    {
        base.Awake();
        targetButtonControllers = GetComponentsInChildren<TargetButtonController>();
        DisableButtons();
    }

    private void DisableButtons()
    {
        foreach (TargetButtonController targetButtonController in targetButtonControllers)
        {
            targetButtonController.DisableButton();
        }
    }

    public void SetupTargetButtons(BaseAbility ability)
    {
        DisableButtons();

        int i = 0;
        switch (ability.TeamTargeting)
        {
            case Team.Enemy:
                foreach (UnitController controller in BattleManager.instance.EnemyUnits)
                {
                    if (controller.IsAlive.Value)
                    {
                        targetButtonControllers[i].EnableButton(controller, ability);
                        i++;
                    }
                }
                break;

            case Team.Ally:
                foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
                {
                    if (controller.IsAlive.Value)
                    {
                        targetButtonControllers[i].EnableButton(controller, ability);
                        i++;
                    }
                }
                break;
        }

    }
}
