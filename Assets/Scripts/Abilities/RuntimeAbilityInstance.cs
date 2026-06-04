using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RuntimeAbilityInstance
{
    public BaseAbility Ability;
    public int RemainingCooldownTurns = 0;

    public bool CanUse(UnitController controller)
    {
        if (!Ability.CanUse(controller))
            return false;

        if (RemainingCooldownTurns > 0)
            return false;

        return true;
    }

    public void ConsumeCost(UnitController controller)
    {
        Ability.ConsumeCost(controller);
        RemainingCooldownTurns = Ability.Cooldown;
    }

    public void ProgressCooldown()
    {
        if (RemainingCooldownTurns == 0)
            return;

        RemainingCooldownTurns = Mathf.Clamp(RemainingCooldownTurns - 1, 0, Ability.Cooldown);
    }
}
