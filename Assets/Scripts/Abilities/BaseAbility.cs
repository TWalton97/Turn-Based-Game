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
    public DamageType DamageType;
    public int DamageAmount;
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


