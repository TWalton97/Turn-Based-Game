using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Abilities", menuName = "Abilities")]
public class BaseAbility : ScriptableObject, ICombatActionSource
{
    public string AbilityName;
    [TextArea] public string AbilityDescription;

    public int ManaCost;
    public Team TeamTargeting;  //Used for targeting
    public TargetType TargetType;   //Used for targeting
    public int Cooldown;

    public bool MovesToTarget;
    public float AbilityAnimationDelay = 2f;
    public bool UsesProjectile;
    public AbilityAnimationType AnimationType;
    public GameObject VFX;

    public List<AIIntent> AIIntents;
    public List<AbilityEffect> abilityEffects = new();
    public float[] impactTimings;

    public bool CanUse(UnitController controller)
    {
        return controller.CurrentMana.Value >= ManaCost;
    }

    public void ConsumeCost(UnitController controller)
    {
        controller.ServerUpdateMana(-ManaCost);
    }

    protected virtual void OnEnable()
    {
        AbilityDatabase.RegisterBaseAbility(this);
    }
}

[System.Serializable]
public class AbilityEffect
{
    public DamageType DamageType;

    //Damage effect
    public TargetType TargetType;
    public List<AbilityEffectDamageScaling> DamageScaling;
    public int NumberOfHits;
    public float DurationBetweenHits;

    public StatusEffect StatusToApply;

    [Header("Condition")]
    public EffectType EffectType = EffectType.None;
    public ConditionType ConditionType = ConditionType.None;
    public float ConditionThreshold = 0f;
    public EffectScalingType EffectScalingType = EffectScalingType.None;
    public float ScalingMultiplier = 1f;
}

[Serializable]
public class AbilityEffectDamageScaling
{
    public StatType Attribute;
    public float ScalingAmount;
}

public enum Team
{
    Ally,   //Selected target must be on the user's team
    Enemy,  //Selceted targeted must not be on the user's team
}

public enum TargetType
{
    SingleUnit, //Selected Target
    AllUnits,   //All units on both teams
    CasterTeam, //All units on the team of the user
    TargetTeam, //All untis on the team of the target
    Self    //Just the caster
}

public enum DamageType
{
    None,
    Physical,
    Fire,
    Cold,
    Psychic,
    Heal
}

[System.Serializable]
public enum ConditionType
{
    None,

    // success checks
    RequiresAnyHit,
    RequiresAnyCrit,
    RequiresAllMissed,
    RequiresNoDodges,

    // result-based checks
    RequiresDamageDealt,
    RequiresHealingDone,

    // contextual
    RequiresPreviousEffectSuccess
}

[System.Serializable]
public enum EffectType
{
    None,
    Damage,
    Heal,
    ApplyStatus,
    Shield,
    ManaRestore
}

[System.Serializable]
public enum EffectScalingType
{
    None,

    BasedOnPreviousDamage,
    BasedOnPreviousHealing,
    BasedOnCritCount,

    BasedOnAttributeScaling,
}

[System.Serializable]
public enum AIIntent
{
    Damage,
    Heal,
    SelfHeal,
    AOEDamage,

}

[Serializable]
public enum AbilityAnimationType
{
    LightAttack,
    HeavyAttack,
    Spell,
    Heal,
}




