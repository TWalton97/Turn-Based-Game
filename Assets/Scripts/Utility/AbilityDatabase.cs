using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class AbilityDatabase
{
    public static Dictionary<FixedString64Bytes, BaseAbility> BaseAbilities = new();

    public static BaseAbility GetAbilityByName(FixedString64Bytes abilityName)
    {
        if (BaseAbilities.TryGetValue(abilityName, out var ability))
            return ability;

        Debug.LogError($"Status '{abilityName}' not found.");
        return null;
    }

    public static void RegisterBaseAbility(BaseAbility baseAbility)
    {
        if (string.IsNullOrEmpty(baseAbility.AbilityName))
            return;

        FixedString64Bytes fixedName = baseAbility.AbilityName;

        if (BaseAbilities.ContainsKey(fixedName))
            return;

        if (BaseAbilities.ContainsValue(baseAbility))
            return;

        BaseAbilities.Add(fixedName, baseAbility);
    }
}