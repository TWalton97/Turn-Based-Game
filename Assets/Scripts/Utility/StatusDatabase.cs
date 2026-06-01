using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StatusDatabase
{
    public static Dictionary<string, StatusEffect> StatusEffects = new();

    public static StatusEffect GetStatusByName(string statusName)
    {
        return StatusEffects[statusName];
    }

    public static void RegisterStatusEffect(StatusEffect statusEffect)
    {
        if (StatusEffects.ContainsValue(statusEffect))
            return;

        StatusEffects.Add(statusEffect.StatusEffectName, statusEffect);
    }
}
