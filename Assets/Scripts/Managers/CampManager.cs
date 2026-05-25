using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CampManager : MonoBehaviour
{
    public static CampManager instance;

    public UnitController TrackedUnitController;
    public PlayerDataController playerDataController;

    //Ability List
    public CampAbilityEntry AbilityEntry;
    public Transform AbilityEntriesParent;

    //Details Panel
    public TextMeshProUGUI DetailsTitle;
    public TextMeshProUGUI DetailsInfo;
    public TextMeshProUGUI DetailsStats;

    public TextMeshProUGUI PlayerStatsPanel;
    public TextMeshProUGUI InvestPointsButton;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void PopulateCampUI(UnitController unitController)
    {
        //Populate ability list
        //Populate inventory
        //Populate equipped gear
        //Populate stats

        TrackedUnitController = unitController;
        playerDataController = unitController.GetComponent<PlayerDataController>();


        PopulateAbilityList();
        PopulatePlayerStatsPanel(unitController, playerDataController.PlayerStats);
    }

    private void PopulateAbilityList()
    {
        for (int i = 0; i < TrackedUnitController.Abilities.Count; i++)
        {
            BaseAbility ability = TrackedUnitController.Abilities[i];
            CampAbilityEntry entry = Instantiate(AbilityEntry, AbilityEntriesParent);
            entry.AssignAbilityToButton(ability);
            entry.GetComponent<Button>().onClick.AddListener(() =>
            {
                PopulateDetailsPanel(ability.name, ability.AbilityDescription, ability.DamageAmount.ToString());
            });
        }
    }

    public void PopulateDetailsPanel(string title, string info, string stats)
    {
        DetailsTitle.text = title;
        DetailsInfo.text = info;
        DetailsStats.text = stats;
    }

    public void PopulatePlayerStatsPanel(UnitController controller, PlayerStats playerStats)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"HP: {controller.CurrentHealth}/{controller.MaxHealth}");
        sb.AppendLine($"Energy: {controller.MaxMana}");
        sb.AppendLine($"Level: {playerStats.Level} ({playerStats.CurrentExp}/{30})");
        sb.AppendLine();
        sb.AppendLine($"STR: {playerStats.Strength}");
        sb.AppendLine($"DEX: {playerStats.Dexterity}");
        sb.AppendLine($"CON: {playerStats.Constitution}");
        sb.AppendLine($"INT: {playerStats.Intelligence}");
        sb.AppendLine($"FTH: {playerStats.Faith}");
        sb.AppendLine($"LCK: {playerStats.Luck}");

        sb.AppendLine($"Initiative: {playerStats.InitiativeMin} - {playerStats.InitiativeMax}");
        sb.AppendLine($"Crit Chance: {playerStats.CritChance}%");
        sb.AppendLine($"Crit Damage: {playerStats.CritDamage}%");
        sb.AppendLine($"Block Chance: {playerStats.BlockChance}%");
        sb.AppendLine($"Block Damage Reduction: {playerStats.BlockDamageReduction}%");
        sb.AppendLine($"Dodge Chance: {playerStats.DodgeChance}%");
        sb.AppendLine($"Aggro: {playerStats.Aggro}%");
        sb.AppendLine($"Lifesteal: {playerStats.Lifesteal}%");
        sb.AppendLine($"Energy Gain: {playerStats.EnergyGain}%");
        sb.AppendLine($"Lucky Drop: {playerStats.LuckyDrop}%");
        sb.AppendLine($"Incoming Healing: {playerStats.IncomingHealing}%");
        sb.AppendLine($"Outgoing Healing: {playerStats.OutgoingHealing}%");

        PlayerStatsPanel.text = sb.ToString();

        InvestPointsButton.text = $"Invest Points ({playerStats.AvailableStatPoints})";
    }
}
