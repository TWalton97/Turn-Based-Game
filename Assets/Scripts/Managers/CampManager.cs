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
    public TextMeshProUGUI UnlockAbilitiesButton;

    //Equipped Gear
    public List<EquippedGearSlot> EquippedGearSlots;

    //Crafting
    public List<ItemSO> CollectedItems; //Every time an item is collected, we add it here
    public List<RecipeSO> AllRecipes;
    public Transform CraftingEntriesParent;
    public CraftingEntry CraftingEntryPrefab;
    private List<CraftingEntry> createdCraftingEntries = new();
    private bool recipeEntriesCreated = false;

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
        PopulateCraftingMenu();

        playerDataController.OnInventoryUpdated += PopulateItemList;
    }

    void OnDestroy()
    {
        if (playerDataController != null)
            playerDataController.OnInventoryUpdated -= PopulateItemList;
    }

    public void PopulateAbilityList()
    {
        foreach (Transform child in AbilityEntriesParent)
        {
            Destroy(child.gameObject);
        }
        CampAbilityEntries.Clear();

        for (int i = 0; i < TrackedUnitController.RuntimeAbilityInstances.Count; i++)
        {
            RuntimeAbilityInstance abilityInstance = TrackedUnitController.RuntimeAbilityInstances[i];
            CampAbilityEntry entry = Instantiate(AbilityEntry, AbilityEntriesParent);
            CampAbilityEntries.Add(entry);
            entry.AssignAbilityToButton(abilityInstance.Ability);
            entry.GetComponent<Button>().onClick.AddListener(() =>
            {
                PopulateDetailsPanel(abilityInstance.Ability.AbilityName, abilityInstance.Ability.AbilityDescription, abilityInstance.Ability.abilityEffects[0].DamageAmount.ToString());
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
            if (!playerDataController.IsItemEquipped(playerDataController.InventoryItems[i].id))
            {
                CampItemEntry itemEntry = Instantiate(CampItemEntry, ItemEntriesParent);
                CampItemEntries.Add(itemEntry);
                itemEntry.AssignItem(playerDataController.InventoryItems[i]);
                if (!CollectedItems.Contains(playerDataController.InventoryItems[i].Item))
                    CollectedItems.Add(playerDataController.InventoryItems[i].Item);
                itemEntry.InspectButton.onClick.AddListener(() =>
                {
                    PopulateDetailsPanel(itemEntry.InventoryEntry.Item.ItemName,
                    itemEntry.InventoryEntry.Item.ItemInformation,
                    itemEntry.InventoryEntry.Item.Description);
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

    public void PopulateCraftingMenu()
    {
        if (!recipeEntriesCreated)
        {
            foreach (RecipeSO recipe in AllRecipes)
            {
                CraftingEntry entry = Instantiate(CraftingEntryPrefab, CraftingEntriesParent);
                entry.AssignRecipe(recipe);
                createdCraftingEntries.Add(entry);
            }
            recipeEntriesCreated = true;
        }

        foreach (CraftingEntry entry in createdCraftingEntries)
        {
            entry.CheckIfRecipeUnlocked();
        }
    }

    public void PopulatePlayerStatsPanel()
    {
        UnitController controller = TrackedUnitController;
        PlayerStats playerStats = playerDataController.PlayerStats;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"HP: {controller.CurrentHealth}/{controller.MaxHealth}");
        sb.AppendLine($"Energy: {controller.MaxMana}");
        sb.AppendLine($"Level: {controller.Level} ({playerStats.CurrentExp}/{30})");
        sb.AppendLine();
        sb.AppendLine($"STR: {controller.GetStatType(StatType.STR)}");
        sb.AppendLine($"DEX: {controller.GetStatType(StatType.DEX)}");
        sb.AppendLine($"CON: {controller.GetStatType(StatType.CON)}");
        sb.AppendLine($"INT: {controller.GetStatType(StatType.INT)}");
        sb.AppendLine($"FTH: {controller.GetStatType(StatType.FTH)}");
        sb.AppendLine($"LCK: {controller.GetStatType(StatType.LCK)}");

        sb.AppendLine($"Initiative: {controller.GetStatType(StatType.InitiativeMin)} - {controller.GetStatType(StatType.InitiativeMax)}");
        sb.AppendLine($"Crit Chance: {controller.GetStatType(StatType.CritChance)}%");
        sb.AppendLine($"Crit Damage: {controller.GetStatType(StatType.CritDamage)}%");
        sb.AppendLine($"Block Chance: {controller.GetStatType(StatType.BlockChance)}%");
        sb.AppendLine($"Block Damage Reduction: {controller.GetStatType(StatType.BlockDamageReduction)}%");
        sb.AppendLine($"Dodge Chance: {controller.GetStatType(StatType.DodgeChance)}%");
        sb.AppendLine($"Aggro: {controller.GetStatType(StatType.Aggro)}%");
        sb.AppendLine($"Lifesteal: {controller.GetStatType(StatType.Lifesteal)}%");
        sb.AppendLine($"Energy Gain: {controller.GetStatType(StatType.EnergyGain)}%");
        sb.AppendLine($"Lucky Drop: {controller.GetStatType(StatType.LuckyDrop)}%");
        sb.AppendLine($"Incoming Healing: {controller.GetStatType(StatType.IncomingHealing)}%");
        sb.AppendLine($"Outgoing Healing: {controller.GetStatType(StatType.OutgoingHealing)}%");

        PlayerStatsPanel.text = sb.ToString();

        InvestPointsButton.text = $"Invest Points ({playerStats.AvailableStatPoints})";
        UnlockAbilitiesButton.text = $"Unlock Abilities ({playerDataController.PendingAbilityUnlocks.Count})";
    }

    public void TryEquipItem(CampItemEntry entry)
    {
        foreach (EquippedGearSlot slot in EquippedGearSlots)
        {
            EquipmentItemSO equipmentItemSO = entry.InventoryEntry.Item as EquipmentItemSO;
            if (slot.EquipmentSlot == equipmentItemSO.EquipmentSlot)
            {
                slot.EquipItemToSlot(entry.InventoryEntry);
                PopulatePlayerStatsPanel();
            }
        }
    }
}
