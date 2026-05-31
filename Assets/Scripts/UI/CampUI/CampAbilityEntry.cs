using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CampAbilityEntry : MonoBehaviour
{
    //Internal Data
    public BaseAbility Ability;
    public TextMeshProUGUI AbilityName;

    public void AssignAbilityToButton(BaseAbility ability)
    {
        Ability = ability;
        AbilityName.text = ability.AbilityName;
    }

    public string GenerateAbilityDescription()
    {
        return AbilityTooltipBuilder.Build(Ability);
    }
}

public static class AbilityTooltipBuilder
{
    public static string Build(BaseAbility ability)
    {
        List<string> lines = new List<string>();

        lines.Add("");

        lines.Add($"Mana Cost: {ability.ManaCost}");
        lines.Add($"Cooldown: {ability.Cooldown}");

        lines.Add("");

        lines.Add($"Targets: {ability.TeamTargeting}");
        lines.Add($"Effects: {System.Text.RegularExpressions.Regex.Replace(ability.TargetType.ToString(), "(\\B[A-Z])", " $1")}");

        lines.Add("");

        foreach (var effect in ability.abilityEffects)
        {
            lines.AddRange(FormatEffect(effect, ability));
        }

        return string.Join("\n", lines).TrimEnd();
    }

    private static List<string> FormatEffect(AbilityEffect effect, BaseAbility ability)
    {
        List<string> lines = new List<string>();

        // 1. Core action line
        string coreLine = BuildCoreLine(effect, ability);

        if (!string.IsNullOrWhiteSpace(coreLine))
            lines.Add(coreLine);

        // 2. Scaling
        string scaling = BuildScalingLine(effect);
        if (!string.IsNullOrEmpty(scaling))
            lines.Add(scaling);

        string status = BuildStatusLine(effect);
        if (!string.IsNullOrEmpty(status))
            lines.Add(status);

        return lines;
    }

    private static string BuildCoreLine(AbilityEffect effect, BaseAbility ability)
    {
        if (effect.DamageType == DamageType.None)
            return "";

        string actionType = effect.DamageType == DamageType.Heal ? $"Heals" : $"Deals";
        string damageType = effect.DamageType == DamageType.Heal ? "" : $"{effect.DamageType} damage ";
        string target = TargetToString(effect.TargetType, ability.TeamTargeting);
        string hits = effect.NumberOfHits > 1 ? $" {effect.NumberOfHits} times" : "";

        string amount = Mathf.Abs(effect.DamageAmount).ToString();
        if (effect.EffectScalingType == EffectScalingType.BasedOnPreviousDamage)
        {
            amount = $"{effect.ScalingMultiplier * 100f}% of damage dealt";
            return $"{actionType} {amount} to {target} {hits}";
        }

        return $"{actionType} {amount} {damageType}to {target}{hits}";
    }

    private static string TargetToString(TargetType target, Team team)
    {
        return target switch
        {
            TargetType.SingleUnit => "target " + team.ToString(),
            TargetType.AllUnits => "all units",
            TargetType.TargetTeam => "target team",
            TargetType.CasterTeam => "all allies",
            TargetType.Self => "self",
            _ => "unknown target"
        };
    }

    private static string BuildScalingLine(AbilityEffect effect)
    {
        List<string> parts = new List<string>();

        if (effect.StrengthScaling != 0)
            parts.Add($"{effect.StrengthScaling * 100f}% STR");

        if (effect.DexterityScaling != 0)
            parts.Add($"{effect.DexterityScaling * 100f}% DEX");

        if (effect.IntelligenceScaling != 0)
            parts.Add($"{effect.IntelligenceScaling * 100f}% INT");

        if (parts.Count == 0)
            return null;

        return "Scaling: " + "\n" + string.Join("\n", parts);
    }

    private static string BuildConditionLine(AbilityEffect effect)
    {
        if (effect.ConditionType == ConditionType.None)
            return null;

        var c = effect.ConditionType;

        switch (c)
        {
            case ConditionType.RequiresAnyHit:
                return $" on hit";

            case ConditionType.RequiresAnyCrit:
                return $" on critical hit";

            case ConditionType.RequiresDamageDealt:
                return $" if at least {effect.ConditionThreshold} damage is dealt";

            default:
                return null;
        }
    }

    private static string BuildStatusLine(AbilityEffect effect)
    {
        if (effect.StatusToApply == null)
            return null;

        return $"Applies {effect.StatusToApply.StatusEffectName}{BuildConditionLine(effect)}";
    }
}
