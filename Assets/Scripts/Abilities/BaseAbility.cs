using System.Collections;
using System.Collections.Generic;
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

    public DamageType DamageType;
    public int DamageAmount;
    public int NumberOfHits;
    public float DurationBetweenHits;

    public float StrengthScaling = 0f;
    public float DexterityScaling = 0f;
    public float IntelligenceScaling = 0f;

    public List<AbilityEffect> abilityEffects = new();

    public bool CanUse(UnitController controller)
    {
        return controller.CurrentMana.Value >= ManaCost;
    }

    public void ConsumeCost(UnitController controller)
    {
        controller.ServerUpdateMana(-ManaCost, true);
    }
}

[System.Serializable]
public class AbilityEffect
{
    public DamageType DamageType;

    //Damage effect
    public TargetType TargetType;
    public int DamageAmount;
    public int NumberOfHits;
    public int DurationBetweenHits;
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
    Heal
}


