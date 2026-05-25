using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkillpointsMenu : MonoBehaviour
{
    public int AvailableSkillpoints;
    public TextMeshProUGUI AvailableSkillpointsMenu;
    public SkillpointSwitch[] SkillpointSwitches;
    public InvestPointsButtonController investPointsButtonController;

    private void Awake()
    {
        SkillpointSwitches = GetComponentsInChildren<SkillpointSwitch>();
    }

    private void OnEnable()
    {
        AvailableSkillpoints = CampManager.instance.playerDataController.PlayerStats.AvailableStatPoints;
        UpdateText();
    }

    public void UpdateText()
    {
        AvailableSkillpointsMenu.text = $"Skillpoints: {AvailableSkillpoints}";
    }

    public void ConfirmChanges()
    {
        PlayerStats playerStats = CampManager.instance.playerDataController.PlayerStats;
        playerStats.AvailableStatPoints = AvailableSkillpoints;
        for (int i = 0; i < SkillpointSwitches.Length - 1; i++)
        {
            switch (SkillpointSwitches[i].Attribute)
            {
                case Attribute.STR:
                    playerStats.Strength += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.DEX:
                    playerStats.Dexterity += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.CON:
                    playerStats.Constitution += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.INT:
                    playerStats.Intelligence += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.FTH:
                    playerStats.Faith += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.CHA:
                    playerStats.Charisma += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.LCK:
                    playerStats.Luck += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
            }
            SkillpointSwitches[i].Reset();
        }
        investPointsButtonController.UpdateText(playerStats.AvailableStatPoints);
    }
}
