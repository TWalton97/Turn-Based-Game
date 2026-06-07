using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnlockAbilitiesMenu : MonoBehaviour, IPointerMoveHandler
{
    public List<CampAbilityEntry> AbilityUnlockEntries;
    public BaseAbility CurrentlySelectedAbility;

    public RectTransform AbilityUnlocksParent;
    public RectTransform DetailsPanelRectTransform;

    private UnitController unitController;
    private PlayerDataController playerDataController;
    public List<AbilityUnlock> PendingAbilityUnlocks;
    public List<BaseAbility> SpawnedBaseAbilities;

    public CampAbilityEntry CampAbilityEntry;

    public TextMeshProUGUI DetailsTitle;
    public TextMeshProUGUI DetailsInfo;
    public TextMeshProUGUI DetailsStats;

    public AbilityUnlockButtonController UnlockAbilitiesButton;
    private TMP_LinkInfo? currentLink;

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
        SpawnedBaseAbilities.Clear();

        foreach (BaseAbility ability in PendingAbilityUnlocks[0].AbilityToUnlock)
        {
            CampAbilityEntry entry = Instantiate(CampAbilityEntry, AbilityUnlocksParent);
            AbilityUnlockEntries.Add(entry);
            entry.AssignAbilityToButton(ability);
            SpawnedBaseAbilities.Add(entry.Ability);
            entry.GetComponent<Button>().onClick.AddListener(() =>
            {
                PopulateAbilityUnlockDetailsPanel(ability.AbilityName,
                ability.AbilityDescription,
                entry.GenerateAbilityDescription());
                CurrentlySelectedAbility = entry.Ability;
            });
        }

        if (playerDataController.UnchosenAbilities.Count > 0)
        {
            int rand = Random.Range(0, playerDataController.UnchosenAbilities.Count);
            BaseAbility ability = playerDataController.UnchosenAbilities[rand];
            SpawnedBaseAbilities.Add(ability);

            CampAbilityEntry entry = Instantiate(CampAbilityEntry, AbilityUnlocksParent);
            AbilityUnlockEntries.Add(entry);
            entry.AssignAbilityToButton(ability);
            entry.GetComponent<Button>().onClick.AddListener(() =>
            {
                PopulateAbilityUnlockDetailsPanel(ability.AbilityName,
                ability.AbilityDescription,
                entry.GenerateAbilityDescription());
                CurrentlySelectedAbility = ability;
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

        int abilityIndex = SpawnedBaseAbilities.IndexOf(CurrentlySelectedAbility);
        unitController.UnlockAbilityServerRpc(SpawnedBaseAbilities[abilityIndex].AbilityName);

        if (playerDataController.UnchosenAbilities.Contains(SpawnedBaseAbilities[abilityIndex]))
            playerDataController.UnchosenAbilities.Remove(SpawnedBaseAbilities[abilityIndex]);

        SpawnedBaseAbilities.RemoveAt(abilityIndex);
        for (int i = 0; i < SpawnedBaseAbilities.Count; i++)
        {
            if (!playerDataController.UnchosenAbilities.Contains(SpawnedBaseAbilities[i]))
                playerDataController.UnchosenAbilities.Add(SpawnedBaseAbilities[i]);
        }

        playerDataController.RemovePendingAbilityUnlock(PendingAbilityUnlocks[0]);
        PendingAbilityUnlocks = playerDataController.PendingAbilityUnlocks;

        CurrentlySelectedAbility = null;
        foreach (Transform child in AbilityUnlocksParent)
        {
            Destroy(child.gameObject);
        }

        SpawnedBaseAbilities.Clear();
        AbilityUnlockEntries.Clear();

        UnlockAbilitiesButton.UpdateText(playerDataController.PendingAbilityUnlocks.Count);

        PopulateAbilityUnlockDetailsPanel("", "", "");

        CampManager.instance.PopulateAbilityList();

        if (PendingAbilityUnlocks.Count == 0)
        {
            gameObject.SetActive(false);
        }
        else
        {
            PopulateNextPendingAbilityUnlock();
        }
    }

    public void PopulateAbilityUnlockDetailsPanel(string title, string info, string stats)
    {
        DetailsTitle.text = title;
        DetailsInfo.text = AddKeywordLinks(info);
        DetailsStats.text = AddKeywordLinks(stats);

        LayoutRebuilder.ForceRebuildLayoutImmediate(DetailsPanelRectTransform);
    }

    private string AddKeywordLinks(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        foreach (var kvp in StatusDatabase.StatusEffects)
        {
            string statusName = kvp.Key.ToString();

            if (!text.Contains(statusName))
                continue;

            text = text.Replace(
            statusName,
            $"<link=\"{statusName}\"><color=#66CCFF>{statusName}</color></link>");
        }
        return text;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        bool hit =
        TryHandleHover(DetailsInfo, eventData) ||
        TryHandleHover(DetailsStats, eventData);

        if (!hit)
        {
            TooltipManager.DisableTooltipIfSource?.Invoke(this);
        }
    }

    private bool TryHandleHover(TMP_Text text, PointerEventData eventData)
    {
        if (text == null)
            return false;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            text,
            eventData.position,
            null);

        if (linkIndex == -1)
            return false;

        TMP_LinkInfo linkInfo = text.textInfo.linkInfo[linkIndex];
        string keyword = linkInfo.GetLinkID();

        if (StatusDatabase.StatusEffects.TryGetValue(
            new FixedString64Bytes(keyword),
            out var status))
        {
            TooltipData tooltipData = new TooltipData(
                status.StatusEffectName,
                "",
                status.StatusDescription,
                ""
            );

            TooltipManager.instance.EnableTooltipAtPosition(
                tooltipData,
                eventData.position + new Vector2(150, 0),
                this);

            return true;
        }

        return false;
    }
}
