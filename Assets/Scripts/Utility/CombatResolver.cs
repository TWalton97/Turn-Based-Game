using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class CombatResolver
{
    public static DamageResult CalculateDamage(UnitController attacker, BaseAbility ability, UnitController target)
    {
        DamageResult result = new DamageResult();

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

}

public class DamageResult
{
    public bool Dodged;
    public bool Crit;

    public float Damage;
}
