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
        AvailableSkillpoints = CampManager.instance.playerDataController.PlayerStats.Value.AvailableStatPoints;
        UpdateText();
    }

    public void UpdateText()
    {
        AvailableSkillpointsMenu.text = $"Skillpoints: {AvailableSkillpoints}";
    }

    public void ConfirmChanges()
    {
        UnitController controller = CampManager.instance.TrackedUnitController;
        PlayerStats playerStats = CampManager.instance.playerDataController.PlayerStats.Value;
        playerStats.AvailableStatPoints = AvailableSkillpoints;
        for (int i = 0; i < SkillpointSwitches.Length - 1; i++)
        {
            switch (SkillpointSwitches[i].Attribute)
            {
                case StatType.STR:
                    controller.UnitStats.Strength += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case StatType.DEX:
                    controller.UnitStats.Dexterity += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case StatType.CON:
                    controller.UnitStats.Constitution += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case StatType.INT:
                    controller.UnitStats.Intelligence += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case StatType.FTH:
                    controller.UnitStats.Faith += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case StatType.CHA:
                    controller.UnitStats.Charisma += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
                case StatType.LCK:
                    controller.UnitStats.Luck += SkillpointSwitches[i].CurrentlyInvestedPoints;
                    break;
            }
        }
        ResetSkillpointSwitches();
        controller.RecalculateAllStats();
        controller.CachedStatsDirty = true;
        CampManager.instance.PopulatePlayerStatsPanel();
        investPointsButtonController.UpdateText(playerStats.AvailableStatPoints);
    }

    public void ResetSkillpointSwitches()
    {
        foreach (SkillpointSwitch skillpointSwitch in SkillpointSwitches)
        {
            skillpointSwitch.Reset();
        }
    }
}
