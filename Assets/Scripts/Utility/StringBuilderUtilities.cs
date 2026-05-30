using System.Text.RegularExpressions;
using UnityEngine;

public static class StringBuilderUtilities
{
    private static readonly Regex PlaceholderRegex =
        new Regex(@"\{(\w+)\}");

    public static string Build(string template, float statusEffectPower)
    {
        return PlaceholderRegex.Replace(template, match =>
        {
            string variableName = match.Groups[1].Value;

            var field = "power";

            if (field != null)
            {
                object value = Mathf.Abs(statusEffectPower);
                return value?.ToString() ?? "";
            }

            return match.Value; // Leave unreplaced if not found
        });
    }
}
