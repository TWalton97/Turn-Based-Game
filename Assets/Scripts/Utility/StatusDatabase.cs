using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class StatusDatabase
{
    public static Dictionary<FixedString64Bytes, StatusEffect> StatusEffects = new();

    public static StatusEffect GetStatusByName(FixedString64Bytes statusName)
    {
        if (StatusEffects.TryGetValue(statusName, out var status))
            return status;

        Debug.LogError($"Status '{statusName}' not found.");
        return null;
    }

    public static void RegisterStatusEffect(StatusEffect statusEffect)
    {
        if (string.IsNullOrEmpty(statusEffect.StatusEffectName))
            return;

        FixedString64Bytes fixedName = statusEffect.StatusEffectName;

        if (StatusEffects.ContainsKey(fixedName))
            return;

        if (StatusEffects.ContainsValue(statusEffect))
            return;

        StatusEffects.Add(fixedName, statusEffect);
    }
}
