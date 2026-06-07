using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Abilities", menuName = "Abilities/RandomEffectAbility")]
public class RandomEffectAbility : BaseAbility
{
    public List<StatusEffect> possibleEffects;
}
