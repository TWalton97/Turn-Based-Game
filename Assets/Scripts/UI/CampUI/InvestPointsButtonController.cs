using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InvestPointsButtonController : MonoBehaviour
{
    private TextMeshProUGUI InvestPointsText;
    public GameObject SpendSkillPointsMenu;

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
        UpdateText(CampManager.instance.playerDataController.PlayerStats.AvailableStatPoints);
    }

    public void ToggleSpendSkillpointsMenu()
    {
        SpendSkillPointsMenu.SetActive(!SpendSkillPointsMenu.activeSelf);
    }
}

public enum Attribute
{
    STR,
    DEX,
    CON,
    INT,
    FTH,
    CHA,
    LCK
}
