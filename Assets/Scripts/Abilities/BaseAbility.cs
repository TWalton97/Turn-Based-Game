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
    public bool UsesProjectile;
    public Animation Animation;
    public GameObject VFX;

    public List<AIIntent> AIIntents;

    public List<AbilityEffect> abilityEffects = new();

    public bool CanUse(UnitController controller)
    {
        return controller.CurrentMana.Value >= ManaCost;
    }

    public void ConsumeCost(UnitController controller)
    {
        if (controller.enemyController == null)
        {
            controller.ServerUpdateMana(-ManaCost, true);
        }
        else
        {
            controller.ServerUpdateMana(-ManaCost);
        }
    }
}

[System.Serializable]
public class AbilityEffect
{
    public DamageType DamageType;

    //Damage effect
    public TargetType TargetType;
    public float DamageAmount;
    public int NumberOfHits;
    public float DurationBetweenHits;

    public float StrengthScaling = 0f;
    public float DexterityScaling = 0f;
    public float IntelligenceScaling = 0f;

    public StatusEffect StatusToApply;

    [Header("Condition")]
    public EffectType EffectType = EffectType.None;
    public ConditionType ConditionType = ConditionType.None;
    public float ConditionThreshold = 0f;
    public EffectScalingType EffectScalingType = EffectScalingType.None;
    public float ScalingMultiplier = 1f;
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
}

[System.Serializable]
public enum AIIntent
{
    Damage,
    Heal,
    SelfHeal,
    AOEDamage,

}




