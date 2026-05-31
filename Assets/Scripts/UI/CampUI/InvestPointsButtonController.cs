using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InvestPointsButtonController : MonoBehaviour
{
    private TextMeshProUGUI InvestPointsText;
    public SkillpointsMenu SpendSkillPointsMenu;

    void Awake()
    {
        InvestPointsText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void UpdateText(int points)
    {
        InvestPointsText.text = $"Invest Points ({points})";
    }

    private void UpdateText()
    {
        UpdateText(CampManager.instance.playerDataController.PlayerStats.Value.AvailableStatPoints);
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
    MaximumMana
}
