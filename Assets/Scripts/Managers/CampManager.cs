using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CampManager : MonoBehaviour
{
    public static CampManager instance;

    public UnitController TrackedUnitController;
    public PlayerDataController playerDataController;

    //Ability List
    public CampAbilityEntry AbilityEntry;
    public Transform AbilityEntriesParent;
    public List<CampAbilityEntry> CampAbilityEntries;

    //Item List
    public CampItemEntry CampItemEntry;
    public Transform ItemEntriesParent;
    public List<CampItemEntry> CampItemEntries;

    //Details Panel
    public TextMeshProUGUI DetailsTitle;
    public TextMeshProUGUI DetailsInfo;
    public TextMeshProUGUI DetailsStats;

    //Stats Panel
    public TextMeshProUGUI PlayerStatsPanel;
    public TextMeshProUGUI InvestPointsButton;

    //Equipped Gear
    public List<EquippedGearSlot> EquippedGearSlots;
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
        PopulateItemList();
        PopulatePlayerStatsPanel();
    }

    private void PopulateAbilityList()
    {
        foreach (Transform child in AbilityEntriesParent)
        {
            Destroy(child.gameObject);
        }
        CampAbilityEntries.Clear();

        for (int i = 0; i < TrackedUnitController.Abilities.Count; i++)
        {
            BaseAbility ability = TrackedUnitController.Abilities[i];
            CampAbilityEntry entry = Instantiate(AbilityEntry, AbilityEntriesParent);
            CampAbilityEntries.Add(entry);
            entry.AssignAbilityToButton(ability);
            entry.GetComponent<Button>().onClick.AddListener(() =>
            {
                PopulateDetailsPanel(ability.name, ability.AbilityDescription, ability.DamageAmount.ToString());
            });
        }
    }

    public void PopulateItemList()
    {
        foreach (Transform child in ItemEntriesParent)
        {
            Destroy(child.gameObject);
        }
        CampItemEntries.Clear();

        for (int i = 0; i < playerDataController.InventoryItems.Count; i++)
        {
            if (!playerDataController.EquippedItems.Contains(playerDataController.InventoryItems[i].Item as EquipmentItemSO))
            {
                CampItemEntry itemEntry = Instantiate(CampItemEntry, ItemEntriesParent);
                CampItemEntries.Add(itemEntry);
                itemEntry.AssignItem(playerDataController.InventoryItems[i].Item);
                itemEntry.InspectButton.onClick.AddListener(() =>
                {
                    PopulateDetailsPanel(itemEntry.Item.ItemName,
                    itemEntry.Item.ItemInformation,
                    itemEntry.Item.Description);
                });
            }
        }
    }

    public void PopulateDetailsPanel(string title, string info, string stats)
    {
        DetailsTitle.text = title;
        DetailsInfo.text = info;
        DetailsStats.text = stats;
    }

    public void PopulatePlayerStatsPanel()
    {
        UnitController controller = TrackedUnitController;
        PlayerStats playerStats = playerDataController.PlayerStats;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"HP: {controller.CurrentHealth}/{controller.MaxHealth}");
        sb.AppendLine($"Energy: {controller.MaxMana}");
        sb.AppendLine($"Level: {playerStats.Level} ({playerStats.CurrentExp}/{30})");
        sb.AppendLine();
        sb.AppendLine($"STR: {controller.UnitStats.Strength}");
        sb.AppendLine($"DEX: {controller.UnitStats.Dexterity}");
        sb.AppendLine($"CON: {controller.UnitStats.Constitution}");
        sb.AppendLine($"INT: {controller.UnitStats.Intelligence}");
        sb.AppendLine($"FTH: {controller.UnitStats.Faith}");
        sb.AppendLine($"LCK: {controller.UnitStats.Luck}");

        sb.AppendLine($"Initiative: {controller.CombatStats.InitiativeMin} - {controller.CombatStats.InitiativeMax}");
        sb.AppendLine($"Crit Chance: {controller.CombatStats.CritChance}%");
        sb.AppendLine($"Crit Damage: {controller.CombatStats.CritDamage}%");
        sb.AppendLine($"Block Chance: {controller.CombatStats.BlockChance}%");
        sb.AppendLine($"Block Damage Reduction: {controller.CombatStats.BlockDamageReduction}%");
        sb.AppendLine($"Dodge Chance: {controller.CombatStats.DodgeChance}%");
        sb.AppendLine($"Aggro: {controller.CombatStats.Aggro}%");
        sb.AppendLine($"Lifesteal: {controller.CombatStats.Lifesteal}%");
        sb.AppendLine($"Energy Gain: {controller.CombatStats.EnergyGain}%");
        sb.AppendLine($"Lucky Drop: {controller.CombatStats.LuckyDrop}%");
        sb.AppendLine($"Incoming Healing: {controller.CombatStats.IncomingHealing}%");
        sb.AppendLine($"Outgoing Healing: {controller.CombatStats.OutgoingHealing}%");

        PlayerStatsPanel.text = sb.ToString();

        InvestPointsButton.text = $"Invest Points ({playerStats.AvailableStatPoints})";
    }

    public void TryEquipItem(CampItemEntry entry, EquipmentItemSO item)
    {
        foreach (EquippedGearSlot slot in EquippedGearSlots)
        {
            if (slot.EquipmentSlot == item.EquipmentSlot)
            {
                slot.EquipItemToSlot(item);
                PopulatePlayerStatsPanel();
            }
        }
    }
}
