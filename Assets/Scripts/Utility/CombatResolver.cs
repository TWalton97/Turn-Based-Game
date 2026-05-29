using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public static class CombatResolver
{
    public static HitResult CalculateHitDamage(UnitController attacker, AbilityEffect abilityEffect, UnitController target, int resolvedPower)
    {
        HitResult result = new HitResult();

        if (resolvedPower < 0)
        {
            float heal = resolvedPower
            + (abilityEffect.StrengthScaling * attacker.UnitStats.Strength)
            + (abilityEffect.DexterityScaling * attacker.UnitStats.Dexterity)
            + (abilityEffect.IntelligenceScaling * attacker.UnitStats.Intelligence);
            result.Dodged = false;
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
        float damage = resolvedPower
        + (abilityEffect.StrengthScaling * attacker.UnitStats.Strength)
        + (abilityEffect.DexterityScaling * attacker.UnitStats.Dexterity)
        + (abilityEffect.IntelligenceScaling * attacker.UnitStats.Intelligence);

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

        result.DamageType = abilityEffect.DamageType;
        result.Damage = Mathf.Round(damage * 10f / 10f);
        return result;
    }

    public static AbilityResult ResolveAbility(UnitController user, BaseAbility ability, UnitController primaryTarget)
    {
        AbilityResult abilityResult = new AbilityResult();

        ulong userId = user.NetworkObjectId;
        int abilityIndex = user.GetAbilityIndex(ability);
        ulong targetId = primaryTarget.NetworkObjectId;

        AbilityExecutionContext context = new AbilityExecutionContext();
        context.user = user;
        context.ability = ability;
        context.target = primaryTarget;

        AbilityEffectResult[] abilityEffectResults = new AbilityEffectResult[ability.abilityEffects.Count];
        //For each effect we create an ability effect result
        for (int p = 0; p < ability.abilityEffects.Count; p++)
        {
            abilityEffectResults[p] = new AbilityEffectResult();
            abilityEffectResults[p].TargetResults = new TargetResult[0];

            AbilityEffect effect = ability.abilityEffects[p];

            List<UnitController> abilityEffectTargets = ReturnTargetsForAbilityEffect(user, effect, primaryTarget);

            TargetResult[] targetResults = new TargetResult[abilityEffectTargets.Count];

            if (!EffectConditionEvaluator.CheckCondition(ability.abilityEffects[p], context))
                continue;

            //For each target of the effect, we create a target result
            for (int o = 0; o < abilityEffectTargets.Count; o++)
            {
                HitResult[] hitResults = new HitResult[effect.NumberOfHits];

                targetResults[o] = new TargetResult();
                targetResults[o].Hits = new HitResult[0];
                targetResults[o].TargetId = abilityEffectTargets[o].NetworkObjectId;
                targetResults[o].Hits = hitResults;

                if (effect.StatusToApply != null)
                    abilityEffectTargets[o].statusEffectController.AddStatusEffect(effect.StatusToApply);

                //For each hit, we create a hit result
                for (int i = 0; i < effect.NumberOfHits; i++)
                {
                    int resolvedPower = EffectConditionEvaluator.ResolveEffectPower(effect, context);
                    hitResults[i] = CalculateHitDamage(user, effect, abilityEffectTargets[o], resolvedPower);
                    abilityEffectResults[p].TotalDamage += (int)hitResults[i].Damage;

                    if (hitResults[i].Damage != 0)
                    {
                        abilityEffectResults[p].AnyHit = true;
                    }

                    if (hitResults[i].Dodged)
                    {
                        abilityEffectResults[p].AnyDodged = true;
                    }

                    if (hitResults[i].Crit)
                    {
                        abilityEffectResults[p].AnyCrit = true;
                    }
                }


            }


            abilityEffectResults[p].AttackerId = userId;
            abilityEffectResults[p].TargetResults = targetResults;
            context.EffectResults.Add(abilityEffectResults[p]);
        }

        abilityResult.AttackerId = userId;
        abilityResult.AbilityId = abilityIndex;
        abilityResult.TargetId = targetId;
        abilityResult.AbilityEffectResults = abilityEffectResults;
        return abilityResult;
    }

    public static List<UnitController> ReturnTargetsForAbilityEffect(UnitController user, AbilityEffect effect, UnitController primaryTarget)
    {
        List<UnitController> targets = new();
        switch (effect.TargetType)
        {
            case TargetType.SingleUnit:
                targets.Add(primaryTarget);
                break;
            case TargetType.AllUnits:
                targets = BattleManager.instance.AllUnits.Where(u => u.ServerIsAlive.Value).ToList();
                break;
            case TargetType.CasterTeam:
                targets = BattleManager.instance.AllUnits.Where(t => t.UnitTeam == user.UnitTeam && t.ServerIsAlive.Value).ToList();
                break;
            case TargetType.TargetTeam:
                targets = BattleManager.instance.AllUnits.Where(t => t.UnitTeam == primaryTarget.UnitTeam && t.ServerIsAlive.Value).ToList();
                break;
            case TargetType.Self:
                targets.Add(user);
                break;
        }

        return targets;
    }
}

[System.Serializable]
public struct AbilityResult : INetworkSerializable
{
    public ulong AttackerId;
    public int AbilityId;
    public ulong TargetId;

    public AbilityEffectResult[] AbilityEffectResults;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref AttackerId);
        serializer.SerializeValue(ref AbilityId);
        serializer.SerializeValue(ref TargetId);
        serializer.SerializeValue(ref AbilityEffectResults);
    }
}

[System.Serializable]
public struct AbilityEffectResult : INetworkSerializable
{
    public ulong AttackerId;
    public TargetResult[] TargetResults;

    public int TotalDamage;
    public int TotalHealing;

    public bool AnyHit;
    public bool AnyCrit;
    public bool AnyDodged;

    public bool ApplyStatusEffect;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref AttackerId);
        serializer.SerializeValue(ref TargetResults);

        serializer.SerializeValue(ref TotalDamage);
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
    public DamageType DamageType;
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

public class AbilityExecutionContext
{
    public UnitController user;
    public BaseAbility ability;
    public UnitController target;
    public List<AbilityEffectResult> EffectResults = new();
}

public static class EffectConditionEvaluator
{
    public static bool CheckCondition(AbilityEffect effect, AbilityExecutionContext ctx)
    {
        if (effect.ConditionType == ConditionType.None)
            return true;

        if (ctx.EffectResults.Count == 0)
            return false;

        AbilityEffectResult last = ctx.EffectResults.Last();

        switch (effect.ConditionType)
        {
            case ConditionType.RequiresAnyHit:
                return last.AnyHit;

            case ConditionType.RequiresAnyCrit:
                return last.AnyCrit;

            case ConditionType.RequiresDamageDealt:
                return last.TotalDamage > effect.ConditionThreshold;

            case ConditionType.RequiresPreviousEffectSuccess:
                return last.AnyHit;

            default:
                return true;
        }
    }

    public static int ResolveEffectPower(AbilityEffect effect, AbilityExecutionContext ctx)
    {
        int value = effect.DamageAmount;

        if (effect.EffectScalingType == EffectScalingType.BasedOnPreviousDamage)
        {
            if (ctx.EffectResults.Count > 0)
            {
                AbilityEffectResult last = ctx.EffectResults.Last();

                int scaledValue = Mathf.RoundToInt(last.TotalDamage * effect.ScalingMultiplier);

                value = scaledValue * (int)Mathf.Sign(effect.DamageAmount);
            }
        }

        return value;
    }
}




