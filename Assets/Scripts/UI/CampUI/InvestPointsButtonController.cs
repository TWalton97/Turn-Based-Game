using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InvestPointsButtonController : MonoBehaviour
{
    public Image buttonImage;
    public TextMeshProUGUI InvestPointsText;
    public SkillpointsMenu SpendSkillPointsMenu;

    public Color NoPointsColor;
    public Color AvailablePointsColor;

    public void UpdateText(int points)
    {
        InvestPointsText.text = $"Invest Points ({points})";
        buttonImage.color = points == 0 ? NoPointsColor : AvailablePointsColor;
    }

    public void ToggleSpendSkillpointsMenu()
    {
        SpendSkillPointsMenu.gameObject.SetActive(!SpendSkillPointsMenu.gameObject.activeSelf);
        SpendSkillPointsMenu.ResetSkillpointSwitches();
    }
}

public enum StatType
{
    STR,
    DEX,
    CON,
    INT,
    FTH,
    CHA,
    LCK,

    InitiativeMin,
    InitiativeMax,
    CritChance,
    CritDamage,
    BlockChance,
    BlockDamageReduction,
    DodgeChance,
    Aggro,
    Lifesteal,
    EnergyGain,
    LuckyDrop,
    IncomingHealing,
    OutgoingHealing,

    IncomingDamage,
    OutgoingDamage,

    CurrentMana,
    MaximumMana,
    CurrentHealth,
}
