using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Abilities", menuName = "Abilities")]
public class BaseAbility : ScriptableObject
{
    public string AbilityName;
    [TextArea] public string AbilityDescription;

    public int ManaCost;
    public Team TeamTargeting;
    public TargetType TargetType;
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
}

public enum Team
{
    Ally,
    Enemy,
}

public enum TargetType
{
    SingleUnit,
    AllUnits,
    Self
}

public enum DamageType
{
    None,
    Physical,
    Fire,
    Cold,
    Heal
}


