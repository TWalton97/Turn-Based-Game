using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CampManager : NetworkBehaviour
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

    public Transform StashEntriesParent;
    public List<CampItemEntry> CampStashEntries;

    //Details Panel
    public RectTransform DetailPanelRectTransform;
    public TextMeshProUGUI DetailsTitle;
    public TextMeshProUGUI DetailsInfo;
    public TextMeshProUGUI DetailsStats;

    //Stats Panel
    public TextMeshProUGUI PlayerStatsPanel;
    public InvestPointsButtonController InvestPointsButton;
    public AbilityUnlockButtonController UnlockAbilitiesButton;

    //Equipped Gear
    public List<EquippedGearSlot> EquippedGearSlots;

    //Crafting
    public List<ItemSO> CollectedItems; //Every time an item is collected, we add it here
    public List<RecipeSO> AllRecipes;
    public Transform CraftingEntriesParent;
    public CraftingEntry CraftingEntryPrefab;
    private List<CraftingEntry> createdCraftingEntries = new();
    private bool recipeEntriesCreated = false;

    public TextMeshProUGUI ReadyButtonText;

    public NetworkVariable<int> NumberOfReadyVotes;
    HashSet<ulong> votedClients = new();
    HashSet<ulong> expectedVoters = new();

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void LoadCamp()
    {
        if (IsServer)
        {
            votedClients.Clear();
            expectedVoters.Clear();
            NumberOfReadyVotes.Value = 0;
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                expectedVoters.Add(client.Key);
            }
        }

        EnterCampClientRpc();
    }

    [ClientRpc]
    void EnterCampClientRpc()
    {

        foreach (UnitController unit in BattleManager.instance.FriendlyUnits)
        {
            if (unit.IsOwner)
            {
                PopulateCampUI(unit);
                UIManager.instance.EnableCampUI();
                return;
            }
        }
    }

    public void VoteReady()
    {
        RequestCampReadyVoteServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestCampReadyVoteServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (votedClients.Contains(clientId))
            return;

        votedClients.Add(clientId);
        NumberOfReadyVotes.Value = votedClients.Count;

        CountReadyVotes();
    }

    private void CountReadyVotes()
    {
        if (!IsServer)
            return;

        if (votedClients.Count == expectedVoters.Count)
        {
            StartCoroutine(ProgressionManager.instance.TransitionToNextRoom());
        }
    }

    public void PopulateCampUI(UnitController unitController)
    {
        TrackedUnitController = unitController;
        playerDataController = unitController.GetComponent<PlayerDataController>();

        PopulateAbilityList();
        PopulateItemList();
        PopulatePlayerStatsPanel();
        PopulateCraftingMenu();
        PopulateStashList();
        UpdateReadyButtonText(0, 0);

        NumberOfReadyVotes.OnValueChanged += UpdateReadyButtonText;

        TrackedUnitController.Strength.OnValueChanged += OnStatsChanged;
        TrackedUnitController.Dexterity.OnValueChanged += OnStatsChanged;
        TrackedUnitController.Constitution.OnValueChanged += OnStatsChanged;
        TrackedUnitController.Intelligence.OnValueChanged += OnStatsChanged;
        TrackedUnitController.Faith.OnValueChanged += OnStatsChanged;
        TrackedUnitController.Charisma.OnValueChanged += OnStatsChanged;
        TrackedUnitController.Luck.OnValueChanged += OnStatsChanged;

        playerDataController.AvailableStatPoints.OnValueChanged += OnSkillPointsChanged;

        StashController.instance.stashEntries.OnListChanged += UpdateStashEntries;
        playerDataController.InventoryItems.OnListChanged += UpdateInventoryEntries;
        TrackedUnitController.StatModifiers.OnListChanged += UpdateStatsPanel;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        NumberOfReadyVotes.OnValueChanged -= UpdateReadyButtonText;

        if (TrackedUnitController == null)
            return;

        TrackedUnitController.Strength.OnValueChanged -= OnStatsChanged;
        TrackedUnitController.Dexterity.OnValueChanged -= OnStatsChanged;
        TrackedUnitController.Constitution.OnValueChanged -= OnStatsChanged;
        TrackedUnitController.Intelligence.OnValueChanged -= OnStatsChanged;
        TrackedUnitController.Faith.OnValueChanged -= OnStatsChanged;
        TrackedUnitController.Charisma.OnValueChanged -= OnStatsChanged;
        TrackedUnitController.Luck.OnValueChanged -= OnStatsChanged;

        playerDataController.AvailableStatPoints.OnValueChanged -= OnSkillPointsChanged;

        StashController.instance.stashEntries.OnListChanged -= UpdateStashEntries;
        playerDataController.InventoryItems.OnListChanged -= UpdateInventoryEntries;
        TrackedUnitController.StatModifiers.OnListChanged -= UpdateStatsPanel;
    }

    private void UpdateStashEntries(NetworkListEvent<StashEntry> changeEvent)
    {
        PopulateStashList();
    }

    private void UpdateInventoryEntries(NetworkListEvent<InventoryEntry> changeEvent)
    {
        PopulateItemList();
        PopulateEquippedGearPanel();
    }

    private void UpdateStatsPanel(NetworkListEvent<StatModifier> changeEvent)
    {
        PopulatePlayerStatsPanel();
    }

    public void UpdateReadyButtonText(int oldValue, int newValue)
    {
        ReadyButtonText.text = $"Ready ({newValue})";
    }

    public void PopulateAbilityList()
    {
        if (TrackedUnitController.RuntimeAbilityInstances.Count == 0)
            return;

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
                PopulateDetailsPanel(abilityInstance.Ability.AbilityName,
                abilityInstance.Ability.AbilityDescription,
                entry.GenerateAbilityDescription());
            });
        }

        CampAbilityEntry firstEntry = CampAbilityEntries[0];
        PopulateDetailsPanel(CampAbilityEntries[0].Ability.AbilityName,
        CampAbilityEntries[0].Ability.AbilityDescription,
        CampAbilityEntries[0].GenerateAbilityDescription());
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
            if (!playerDataController.IsItemEquipped(playerDataController.InventoryItems[i].instanceId))
            {
                CampItemEntry itemEntry = Instantiate(CampItemEntry, ItemEntriesParent);
                CampItemEntries.Add(itemEntry);
                itemEntry.AssignItem(playerDataController.InventoryItems[i]);
                ItemSO item = ItemDatabase.GetItemByName(playerDataController.InventoryItems[i].itemName.ToString());
                if (!CollectedItems.Contains(item))
                    CollectedItems.Add(item);

                FixedString64Bytes itemId = playerDataController.InventoryItems[i].instanceId;

                itemEntry.InspectButton.onClick.AddListener(() =>
                {
                    PopulateDetailsPanel(item.ItemName,
                    item.Description,
                    itemEntry.GenerateItemDescription());
                });

                itemEntry.TransferButton.onClick.AddListener(() =>
                {
                    StashController.instance.RequestMoveItemToStashServerRpc(TrackedUnitController.NetworkObjectId,
                    itemId);
                });
            }
        }
    }

    public void PopulateStashList()
    {
        foreach (Transform child in StashEntriesParent)
        {
            Destroy(child.gameObject);
        }
        CampStashEntries.Clear();

        for (int i = 0; i < StashController.instance.stashEntries.Count; i++)
        {
            CampItemEntry itemEntry = Instantiate(CampItemEntry, StashEntriesParent);
            CampStashEntries.Add(itemEntry);

            InventoryEntry inventoryEntry = new();
            inventoryEntry.itemName = StashController.instance.stashEntries[i].itemName;
            inventoryEntry.instanceId = StashController.instance.stashEntries[i].instanceId.ToString();
            inventoryEntry.quantity = StashController.instance.stashEntries[i].quantity;
            inventoryEntry.stackable = StashController.instance.stashEntries[i].stackable;
            inventoryEntry.equipped = false;

            itemEntry.AssignItem(inventoryEntry);
            ItemSO item = ItemDatabase.GetItemByName(StashController.instance.stashEntries[i].itemName.ToString());

            CampItemEntry capturedItemEntry = itemEntry;

            itemEntry.InspectButton.onClick.AddListener(() =>
            {
                PopulateDetailsPanel(item.ItemName,
                item.Description,
                itemEntry.GenerateItemDescription());
            });

            itemEntry.TransferButton.onClick.AddListener(() =>
            {
                StashController.instance.RequestMoveItemToInventoryServerRpc(TrackedUnitController.NetworkObjectId,
                capturedItemEntry.InventoryEntry.instanceId);
            });
        }
    }

    public void PopulateDetailsPanel(string title, string info, string stats)
    {
        DetailsTitle.text = title;
        DetailsInfo.text = info;
        DetailsStats.text = stats;

        LayoutRebuilder.ForceRebuildLayoutImmediate(DetailPanelRectTransform);
    }


    public void PopulateEquippedGearPanel()
    {
        foreach (EquippedGearSlot equippedGearSlot in EquippedGearSlots)
        {
            equippedGearSlot.Reset();
        }

        //First we find which items are equipped
        for (int i = 0; i < playerDataController.InventoryItems.Count; i++)
        {
            if (playerDataController.InventoryItems[i].equipped)
            {
                EquipmentItemSO equipmentItemSO = ItemDatabase.GetItemByName(playerDataController.InventoryItems[i].itemName.ToString()) as EquipmentItemSO;
                if (equipmentItemSO == null)
                    break;

                for (int p = 0; p < EquippedGearSlots.Count; p++)
                {
                    if (EquippedGearSlots[p].EquipmentSlot == equipmentItemSO.EquipmentSlot)
                    {
                        EquippedGearSlots[p].EquipItemToSlot(equipmentItemSO);
                        break;
                    }
                }
            }
        }
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

    public void OnStatsChanged(int oldValue, int newValue)
    {
        TrackedUnitController.CachedStatsDirty = true;
        PopulatePlayerStatsPanel();
    }

    public void OnSkillPointsChanged(int oldValue, int newValue)
    {
        InvestPointsButton.UpdateText(newValue);
    }

    public void PopulatePlayerStatsPanel()
    {
        UnitController controller = TrackedUnitController;
        StringBuilder sb = new StringBuilder();

        controller.RecalculateAllStats();

        sb.AppendLine($"HP: {controller.CurrentHealth.Value}/{controller.MaxHealth}");
        sb.AppendLine($"Energy: {controller.MaxMana}");
        sb.AppendLine($"Level: {controller.Level.Value} ({playerDataController.CurrentExp.Value}/{ExperienceValues.ExpToNextLevel[controller.Level.Value]})");
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

        UnlockAbilitiesButton.UpdateText(playerDataController.PendingAbilityUnlocks.Count);
    }

    public void TryEquipItem(InventoryEntry entry)
    {
        playerDataController.TryEquipItemServerRpc(entry.instanceId);
    }

    public void TryUnequipItem(EquipmentItemSO equippedItem)
    {
        playerDataController.TryUnequipItemServerRpc(equippedItem.ItemName);
    }
}
