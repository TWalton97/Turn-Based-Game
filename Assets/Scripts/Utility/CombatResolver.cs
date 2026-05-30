using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public static class CombatResolver
{
    public static HitResult CalculateHitDamage(UnitController attacker, AbilityEffect abilityEffect, UnitController target, float resolvedPower)
    {
        HitResult result = new HitResult();

        if (resolvedPower < 0)
        {
            float heal = resolvedPower
            + (abilityEffect.StrengthScaling * attacker.GetStatType(StatType.STR))
            + (abilityEffect.DexterityScaling * attacker.GetStatType(StatType.DEX))
            + (abilityEffect.IntelligenceScaling * attacker.GetStatType(StatType.INT));
            result.DamageType = abilityEffect.DamageType;
            result.Dodged = false;
            result.Damage = heal;
            result.Damage *= 1 + Random.Range(-0.1f, 0.1f);
            result.Damage = Mathf.Round(result.Damage * 10f) / 10f;
            return result;
        }

        //First we check if the unit dodges
        bool dodged = Random.Range(0f, 100f) < target.GetStatType(StatType.DodgeChance);
        if (dodged)
        {
            result.Dodged = true;
            return result;
        }

        //If it isn't dodged, we calculate the damage
        float damage = resolvedPower
        + (abilityEffect.StrengthScaling * attacker.GetStatType(StatType.STR))
        + (abilityEffect.DexterityScaling * attacker.GetStatType(StatType.DEX))
        + (abilityEffect.IntelligenceScaling * attacker.GetStatType(StatType.INT));

        //Roll if it's a block
        bool blocked = Random.Range(0f, 100f) < target.GetStatType(StatType.BlockChance);
        if (blocked)
        {
            damage *= 1 - (target.GetStatType(StatType.BlockDamageReduction) / 100);
            result.Damage = damage;
            return result;
        }

        //If it isn't blocked, we roll for a crit
        bool crit = Random.Range(0f, 100f) < attacker.GetStatType(StatType.CritChance);
        if (crit)
        {
            result.Crit = true;
            damage *= 1 + (attacker.GetStatType(StatType.CritDamage) / 100);
        }

        //Add final multipliers
        damage *= attacker.GetStatType(StatType.OutgoingDamage) / 100;
        damage *= target.GetStatType(StatType.IncomingDamage) / 100;

        result.DamageType = abilityEffect.DamageType;
        result.Damage = damage;
        result.Damage *= 1 + Random.Range(-0.1f, 0.1f);
        result.Damage = Mathf.Round(result.Damage * 10f) / 10f;
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

                //This is where we actually apply the status effect
                if (effect.StatusToApply != null)
                {
                    float resolvedStatusPower = EffectConditionEvaluator.ResolveEffectPower(effect, context);
                    abilityEffectTargets[o].statusEffectController.AddStatusEffect(effect.StatusToApply, resolvedStatusPower);
                }

                //For each hit, we create a hit result
                for (int i = 0; i < effect.NumberOfHits; i++)
                {
                    float resolvedPower = EffectConditionEvaluator.ResolveEffectPower(effect, context);
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
public class AbilityResult : INetworkSerializable
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
public class AbilityEffectResult : INetworkSerializable
{
    public ulong AttackerId;
    public TargetResult[] TargetResults;

    public float TotalDamage;
    public float TotalHealing;

    public bool AnyHit;
    public bool AnyCrit;
    public bool AnyDodged;

    public bool ApplyStatusEffect;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref AttackerId);
        serializer.SerializeValue(ref TargetResults);

        serializer.SerializeValue(ref TotalDamage);
        serializer.SerializeValue(ref TotalHealing);

        serializer.SerializeValue(ref AnyHit);
        serializer.SerializeValue(ref AnyCrit);
        serializer.SerializeValue(ref AnyDodged);

        serializer.SerializeValue(ref ApplyStatusEffect);
    }
}

[System.Serializable]
public class TargetResult : INetworkSerializable
{
    public ulong TargetId;

    public HitResult[] Hits;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref TargetId);
        serializer.SerializeValue(ref Hits);
    }
}

public class HitResult : INetworkSerializable
{
    public DamageType DamageType;
    public float Damage;
    public bool Crit;
    public bool Dodged;
    public bool Blocked;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref DamageType);
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

    public static float ResolveEffectPower(AbilityEffect effect, AbilityExecutionContext ctx)
    {
        float value = effect.DamageAmount;

        if (effect.EffectScalingType == EffectScalingType.BasedOnPreviousDamage)
        {
            if (ctx.EffectResults.Count > 0)
            {
                AbilityEffectResult last = ctx.EffectResults.Last();

                float scaledValue = last.TotalDamage * effect.ScalingMultiplier;

                value = scaledValue * Mathf.Sign(effect.DamageAmount);
            }
        }

        return value;
    }
}




