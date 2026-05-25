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
        UnitController controller = CampManager.instance.TrackedUnitController;
        PlayerStats playerStats = CampManager.instance.playerDataController.PlayerStats;
        playerStats.AvailableStatPoints = AvailableSkillpoints;
        for (int i = 0; i < SkillpointSwitches.Length - 1; i++)
        {
            switch (SkillpointSwitches[i].Attribute)
            {
                case Attribute.STR:
                    controller.UnitStats.Strength += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.DEX:
                    controller.UnitStats.Dexterity += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.CON:
                    controller.UnitStats.Constitution += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.INT:
                    controller.UnitStats.Intelligence += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.FTH:
                    controller.UnitStats.Faith += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.CHA:
                    controller.UnitStats.Charisma += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case Attribute.LCK:
                    controller.UnitStats.Luck += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
            }
            SkillpointSwitches[i].Reset();
        }
        controller.RecalculateCombatStats();
        CampManager.instance.PopulatePlayerStatsPanel();
        investPointsButtonController.UpdateText(playerStats.AvailableStatPoints);
    }
}
