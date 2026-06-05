using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EventManager : NetworkBehaviour
{
    public static EventManager instance;

    [Header("All Events")]
    public List<EventDefinition> Events;

    [Header("UI - Selection")]
    public GameObject EventSelectionMenu;

    [System.Serializable]
    public class EventSelectionUI
    {
        public Image EventSelectionImage;
        public Button EventSelectionButton;
        public TextMeshProUGUI EventSelectionButtonText;
    }

    public List<EventSelectionUI> EventSelections = new();

    [Header("UI - Event Node")]
    public GameObject StoryEventMenu;
    public Image EventNodeImage;
    public TextMeshProUGUI EventNodeName;
    public TextMeshProUGUI EventNodeDescription;

    public List<EventSelectionUI> EventNodeOptions;

    [Header("Runtime State")]
    public EventDefinition CurrentEvent;
    public EventNode CurrentEventNode;

    private readonly Dictionary<ulong, int> PlayerVotes = new();
    private readonly List<EventDefinition> currentEventOptions = new();
    private readonly Dictionary<int, EventDefinition> SelectedEventOptions = new();
    private readonly Dictionary<int, int> voteCounts = new();

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void LoadEvent()
    {
        PopulateEventOptions();
    }

    #region EVENT GENERATION

    public void PopulateEventOptions()
    {
        if (!IsServer)
            return;

        ServerGenerateEventOptions();
    }

    private void ServerGenerateEventOptions()
    {
        if (Events.Count < 3)
        {
            Debug.LogError("Not enough events to generate options.");
            return;
        }

        int eventA = Random.Range(0, Events.Count);
        int eventB = Random.Range(0, Events.Count);
        int eventC = Random.Range(0, Events.Count);

        ShowEventOptionsClientRpc(eventA, eventB, eventC);
    }

    [ClientRpc]
    private void ShowEventOptionsClientRpc(int eventA, int eventB, int eventC)
    {
        UIManager.instance.EnableEventUI();
        SetupEventSelectionMenu(eventA, eventB, eventC);
    }

    private void SetupEventSelectionMenu(int eventA, int eventB, int eventC)
    {
        CurrentEvent = null;
        CurrentEventNode = null;

        EventSelectionMenu.SetActive(true);
        StoryEventMenu.SetActive(false);

        SelectedEventOptions.Clear();

        SetupEventSelectionUI(0, Events[eventA]);
        SetupEventSelectionUI(1, Events[eventB]);
        SetupEventSelectionUI(2, Events[eventC]);
    }

    #endregion

    #region UI SETUP

    private void SetupEventSelectionUI(int uiIndex, EventDefinition eventDef)
    {
        SelectedEventOptions.Add(uiIndex, eventDef);

        var ui = EventSelections[uiIndex];
        var startNode = eventDef.GetNode(eventDef.startNodeID);

        ui.EventSelectionImage.sprite = startNode.image;
        ui.EventSelectionButtonText.text = $"{eventDef.eventID} (0)";

        ui.EventSelectionButton.onClick.RemoveAllListeners();

        string eventID = eventDef.eventID;

        ui.EventSelectionButton.onClick.AddListener(() =>
        {
            SubmitEventVoteServerRpc(uiIndex);
        });
    }

    #endregion

    #region VOTING

    [ServerRpc(RequireOwnership = false)]
    public void SubmitEventVoteServerRpc(int optionIndex, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        PlayerVotes[senderId] = optionIndex;

        RecalculateVotes();

        PushVoteStateToClients();

        CheckForWinner();
    }

    private void CheckForWinner()
    {
        int requiredVotes = NetworkManager.Singleton.ConnectedClients.Count;

        foreach (var pair in voteCounts)
        {
            if (pair.Value >= requiredVotes)
            {
                ResolveWinningVote(pair.Key);
                return;
            }
        }
    }

    private void RecalculateVotes()
    {
        voteCounts.Clear();

        foreach (var vote in PlayerVotes.Values)
        {
            if (!voteCounts.ContainsKey(vote))
                voteCounts[vote] = 0;

            voteCounts[vote]++;
        }
    }

    [ClientRpc]
    private void UpdateVoteDisplayClientRpc(int[] counts)
    {
        // --- Update Event Selection UI ---
        for (int i = 0; i < EventSelections.Count; i++)
        {
            if (!SelectedEventOptions.ContainsKey(i))
                continue;

            string label = SelectedEventOptions[i].eventID;
            int count = (i < counts.Length) ? counts[i] : 0;

            EventSelections[i].EventSelectionButtonText.text =
                $"{label} ({count})";
        }

        // --- Update Event Node UI ---
        for (int i = 0; i < EventNodeOptions.Count; i++)
        {
            var ui = EventNodeOptions[i];

            if (!ui.EventSelectionButton.gameObject.activeSelf)
                continue;

            string baseLabel = ui.EventSelectionButtonText.text;

            // strip old count safely
            if (baseLabel.Contains("("))
                baseLabel = baseLabel.Split('(')[0].Trim();

            int count = (i < counts.Length) ? counts[i] : 0;

            ui.EventSelectionButtonText.text =
                $"{baseLabel} ({count})";
        }
    }

    private void PushVoteStateToClients()
    {
        int maxOptions = Mathf.Max(EventSelections.Count, EventNodeOptions.Count);

        int[] counts = new int[maxOptions];

        foreach (var kvp in voteCounts)
        {
            if (kvp.Key >= 0 && kvp.Key < maxOptions)
                counts[kvp.Key] = kvp.Value;
        }

        UpdateVoteDisplayClientRpc(counts);
    }

    private void ResolveWinningVote(int winningEventIndex)
    {
        if (CurrentEvent == null)
        {
            EventDefinition winningEvent = SelectedEventOptions[winningEventIndex];

            if (winningEvent == null)
            {
                Debug.LogError($"Winning event not found: {winningEvent.eventID}");
                return;
            }

            LoadEventNodeClientRpc(winningEvent.eventID);
        }
        else
        {
            EventEffectExecuteClientRpc(winningEventIndex);

            EventNode nextNode = CurrentEvent.GetNode(CurrentEventNode.choices[winningEventIndex].nextNodeID);
            if (nextNode != null)
                LoadEventNodeClientRpc(nextNode.nodeID);
        }

        PlayerVotes.Clear();
    }
    #endregion

    #region EVENT LOADING

    [ClientRpc]
    public void EventEffectExecuteClientRpc(int currentEventOptionIndex)
    {
        foreach (var effect in CurrentEventNode.choices[currentEventOptionIndex].effects)
        {
            effect.Execute();
        }
    }

    [ClientRpc]
    private void LoadEventNodeClientRpc(string eventID)
    {
        EventNode startNode = null;

        if (CurrentEvent == null)
        {
            EventDefinition eventDef =
            Events.Find(e => e.eventID == eventID);

            if (eventDef == null)
            {
                Debug.LogError($"Client could not find event: {eventID}");
                return;
            }

            CurrentEvent = eventDef;
            startNode = eventDef.GetNode(eventDef.startNodeID);
        }
        else
        {
            EventNode eventNode = CurrentEvent.GetNode(eventID);

            if (eventNode == null)
            {
                Debug.LogError($"Client could not find event node: {eventID}");
                return;
            }

            startNode = CurrentEvent.GetNode(eventID);
        }

        if (startNode == null)
        {
            Debug.LogError("Next event node is null");
            return;
        }

        LoadEventNode(startNode);
    }

    public void LoadEventNode(EventNode node)
    {
        CurrentEventNode = node;
        EventSelectionMenu.SetActive(false);
        StoryEventMenu.SetActive(true);

        EventNodeImage.sprite = node.image;
        EventNodeName.text = "Event";
        EventNodeDescription.text = $"{node.description}";

        foreach (var option in EventNodeOptions)
            option.EventSelectionButton.gameObject.SetActive(false);


        for (int i = 0; i < node.choices.Count; i++)
        {
            var choice = node.choices[i];
            var ui = EventNodeOptions[i];

            ui.EventSelectionButton.gameObject.SetActive(true);
            ui.EventSelectionButtonText.text = $"{choice.buttonText} (0)";

            ui.EventSelectionButton.onClick.RemoveAllListeners();

            int capturedChoiceIndex = i;

            ui.EventSelectionButton.onClick.AddListener(() =>
            {
                SubmitEventVoteServerRpc(capturedChoiceIndex);
            });
        }
    }

    #endregion
}
