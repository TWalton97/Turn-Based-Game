using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public static class CombatResolver
{
    public static HitResult CalculateHitDamage(UnitController attacker, BaseAbility ability, UnitController target)
    {
        HitResult result = new HitResult();

        if (ability.DamageAmount < 0)
        {
            float heal = ability.DamageAmount
            + (ability.StrengthScaling * attacker.UnitStats.Strength)
            + (ability.DexterityScaling * attacker.UnitStats.Dexterity)
            + (ability.IntelligenceScaling * attacker.UnitStats.Intelligence);
            result.Damage = Mathf.Round(heal * 10f / 10f);
            return result;
        }

        //First we check if the unit dodges
        bool dodged = Random.Range(0f, 100f) < target.CombatStats.DodgeChance;
        if (dodged)
        {
            result.Dodged = true;
            return result;
        }

        //If it isn't dodged, we calculate the damage
        float damage = ability.DamageAmount
        + (ability.StrengthScaling * attacker.UnitStats.Strength)
        + (ability.DexterityScaling * attacker.UnitStats.Dexterity)
        + (ability.IntelligenceScaling * attacker.UnitStats.Intelligence);

        //Roll if it's a block
        bool blocked = Random.Range(0f, 100f) < target.CombatStats.BlockChance;
        if (blocked)
        {
            damage *= 1 - (target.CombatStats.BlockDamageReduction / 100);
            result.Damage = Mathf.Round(damage * 10f / 10f);
            return result;
        }

        //If it isn't blocked, we roll for a crit
        bool crit = Random.Range(0f, 100f) < attacker.CombatStats.CritChance;
        if (crit)
        {
            result.Crit = true;
            damage *= 1 + (attacker.CombatStats.CritDamage / 100);
        }

        result.Damage = Mathf.Round(damage * 10f / 10f);
        return result;
    }

    public static AbilityResult ResolveAbility(UnitController user, BaseAbility ability, List<UnitController> targets)
    {
        AbilityResult abilityResult = new AbilityResult();
        abilityResult.AttackerId = user.NetworkObjectId;
        abilityResult.AbilityId = user.GetAbilityIndex(ability);

        abilityResult.TargetResults = new TargetResult[targets.Count];

        for (int i = 0; i < targets.Count; i++)
        {
            TargetResult targetResult = new TargetResult();

            targetResult.Hits = new HitResult[ability.NumberOfHits];
            targetResult.TargetId = targets[i].NetworkObjectId;

            for (int p = 0; p < ability.NumberOfHits; p++)
            {
                HitResult hitResult = CalculateHitDamage(user, ability, targets[i]);
                targetResult.Hits[p] = hitResult;
            }
            abilityResult.TargetResults[i] = targetResult;
        }
        return abilityResult;
    }

    //We need to deconstruct an ability into it's various ability effects
    //Each ability effect should have its own ability effect result
    //An ability effect result includes a list of target results



}

[System.Serializable]
public struct AbilityResult : INetworkSerializable
{
    public ulong AttackerId;
    public int AbilityId;

    public TargetResult[] TargetResults;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref AttackerId);
        serializer.SerializeValue(ref AbilityId);
        serializer.SerializeValue(ref TargetResults);
    }
}

[System.Serializable]
public struct AbilityEffectResult : INetworkSerializable
{
    public ulong AttackerId;
    public TargetResult[] TargetResults;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref AttackerId);
        serializer.SerializeValue(ref TargetResults);
    }
}

[System.Serializable]
public struct TargetResult : INetworkSerializable
{
    public ulong TargetId;

    public HitResult[] Hits;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref TargetId);
        serializer.SerializeValue(ref Hits);
    }
}

public struct HitResult : INetworkSerializable
{
    public float Damage;
    public bool Crit;
    public bool Dodged;
    public bool Blocked;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Damage);
        serializer.SerializeValue(ref Crit);
        serializer.SerializeValue(ref Dodged);
        serializer.SerializeValue(ref Blocked);
    }
}


