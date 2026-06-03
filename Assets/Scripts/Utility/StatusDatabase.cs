using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class StatusDatabase
{
    public static Dictionary<FixedString64Bytes, StatusEffect> StatusEffects = new();

    public static StatusEffect GetStatusByName(FixedString64Bytes statusName)
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
