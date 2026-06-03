using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnlockAbilitiesMenu : MonoBehaviour
{
    public List<CampAbilityEntry> AbilityUnlockEntries;
    public BaseAbility CurrentlySelectedAbility;

    public RectTransform AbilityUnlocksParent;
    public RectTransform DetailsPanelRectTransform;

    private UnitController unitController;
    private PlayerDataController playerDataController;
    public List<AbilityUnlock> PendingAbilityUnlocks;

    public CampAbilityEntry CampAbilityEntry;

    public TextMeshProUGUI DetailsTitle;
    public TextMeshProUGUI DetailsInfo;
    public TextMeshProUGUI DetailsStats;

    public TextMeshProUGUI UnlockAbilitiesButton;

    //When this opens, we need to get the list of "pending ability unlocks" from the player data controller

    void OnEnable()
    {
        playerDataController = CampManager.instance.playerDataController;
        unitController = playerDataController.UnitController;

        PendingAbilityUnlocks = playerDataController.PendingAbilityUnlocks;

        PopulateNextPendingAbilityUnlock();
    }

    public void PopulateNextPendingAbilityUnlock()
    {
        if (PendingAbilityUnlocks.Count == 0)
            return;

        if (CurrentlySelectedAbility != null)
            return;

        foreach (Transform child in AbilityUnlocksParent)
        {
            Destroy(child.gameObject);
        }
        AbilityUnlockEntries.Clear();

        foreach (BaseAbility ability in PendingAbilityUnlocks[0].AbilityToUnlock)
        {
            CampAbilityEntry entry = Instantiate(CampAbilityEntry, AbilityUnlocksParent);
            AbilityUnlockEntries.Add(entry);
            entry.AssignAbilityToButton(ability);
            entry.GetComponent<Button>().onClick.AddListener(() =>
            {
                PopulateAbilityUnlockDetailsPanel(ability.AbilityName,
                ability.AbilityDescription,
                entry.GenerateAbilityDescription());
                CurrentlySelectedAbility = entry.Ability;
            });
        }

        CampAbilityEntry firstEntry = AbilityUnlockEntries[0];
        PopulateAbilityUnlockDetailsPanel(AbilityUnlockEntries[0].Ability.AbilityName,
        AbilityUnlockEntries[0].Ability.AbilityDescription,
        AbilityUnlockEntries[0].GenerateAbilityDescription());
        CurrentlySelectedAbility = firstEntry.Ability;
    }

    public void ConfirmSelection()
    {
        if (CurrentlySelectedAbility == null)
            return;

        int abilityIndex = PendingAbilityUnlocks[0].AbilityToUnlock.IndexOf(CurrentlySelectedAbility);
        unitController.UnlockAbilityServerRpc(PendingAbilityUnlocks[0].LevelToUnlock, abilityIndex);
        playerDataController.RemovePendingAbilityUnlock(PendingAbilityUnlocks[0]);
        PendingAbilityUnlocks = playerDataController.PendingAbilityUnlocks;

        CurrentlySelectedAbility = null;
        foreach (Transform child in AbilityUnlocksParent)
        {
            Destroy(child.gameObject);
        }
        AbilityUnlockEntries.Clear();

        UnlockAbilitiesButton.text = $"Unlock Abilities ({playerDataController.PendingAbilityUnlocks.Count})";

        PopulateAbilityUnlockDetailsPanel("", "", "");

        CampManager.instance.PopulateAbilityList();

        if (PendingAbilityUnlocks.Count == 0)
            gameObject.SetActive(false);
    }

    public void PopulateAbilityUnlockDetailsPanel(string title, string info, string stats)
    {
        DetailsTitle.text = title;
        DetailsInfo.text = info;
        DetailsStats.text = stats;

        LayoutRebuilder.ForceRebuildLayoutImmediate(DetailsPanelRectTransform);
    }
}
