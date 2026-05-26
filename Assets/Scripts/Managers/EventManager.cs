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

    private readonly Dictionary<ulong, string> PlayerVotes = new();
    private readonly List<EventDefinition> currentEventOptions = new();

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    #region EVENT GENERATION

    public void PopulateEventOptions()
    {
        if (!IsServer)
            return;

        GenerateEventOptions();
    }

    private void GenerateEventOptions()
    {
        if (Events.Count < 3)
        {
            Debug.LogError("Not enough events to generate options.");
            return;
        }

        int a = Random.Range(0, Events.Count);
        int b = Random.Range(0, Events.Count);
        int c = Random.Range(0, Events.Count);

        ShowEventOptionsClientRpc(a, b, c);
    }

    [ClientRpc]
    private void ShowEventOptionsClientRpc(int a, int b, int c)
    {
        SetupSelectionMenu(a, b, c);
    }

    private void SetupSelectionMenu(int a, int b, int c)
    {
        EventSelectionMenu.SetActive(true);
        StoryEventMenu.SetActive(false);

        SetupOptionUI(0, Events[a]);
        SetupOptionUI(1, Events[b]);
        SetupOptionUI(2, Events[c]);
    }

    #endregion

    #region UI SETUP

    private void SetupOptionUI(int uiIndex, EventDefinition eventDef)
    {
        var ui = EventSelections[uiIndex];
        var startNode = eventDef.GetNode(eventDef.startNodeID);

        ui.EventSelectionImage.sprite = startNode.image;
        ui.EventSelectionButtonText.text = eventDef.eventID;

        ui.EventSelectionButton.onClick.RemoveAllListeners();

        string eventID = eventDef.eventID; // IMPORTANT: stable identifier

        ui.EventSelectionButton.onClick.AddListener(() =>
        {
            SubmitVoteServerRpc(eventID);
        });
    }

    #endregion

    #region VOTING

    [ServerRpc(RequireOwnership = false)]
    public void SubmitVoteServerRpc(string eventID, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"Submitting vote to server");

        ulong senderId = rpcParams.Receive.SenderClientId;

        PlayerVotes[senderId] = eventID;

        CheckVotes();
    }

    private void CheckVotes()
    {
        Dictionary<string, int> voteCounts = new();

        foreach (var vote in PlayerVotes.Values)
        {
            if (!voteCounts.ContainsKey(vote))
                voteCounts[vote] = 0;

            voteCounts[vote]++;
        }

        foreach (var pair in voteCounts)
        {
            if (pair.Value >= NetworkManager.Singleton.ConnectedClientsIds.Count)
            {
                ResolveWinningVote(pair.Key);
                return;
            }
        }
    }

    private void ResolveWinningVote(string winningEventID)
    {
        EventDefinition winningEvent =
            Events.Find(e => e.eventID == winningEventID);

        if (winningEvent == null)
        {
            Debug.LogError($"Winning event not found: {winningEventID}");
            return;
        }

        CurrentEvent = winningEvent;

        PlayerVotes.Clear();

        LoadEventNodeClientRpc(winningEventID);
    }

    #endregion

    #region EVENT LOADING

    [ClientRpc]
    private void LoadEventNodeClientRpc(string eventID)
    {
        EventDefinition eventDef =
            Events.Find(e => e.eventID == eventID);

        if (eventDef == null)
        {
            Debug.LogError($"Client could not find event: {eventID}");
            return;
        }

        CurrentEvent = eventDef;

        EventNode startNode = eventDef.GetNode(eventDef.startNodeID);
        LoadEventNode(startNode);
    }

    public void LoadEventNode(EventNode node)
    {
        EventSelectionMenu.SetActive(false);
        StoryEventMenu.SetActive(true);

        EventNodeImage.sprite = node.image;
        EventNodeName.text = "Event";
        EventNodeDescription.text = node.description;

        foreach (var option in EventNodeOptions)
            option.EventSelectionButton.gameObject.SetActive(false);

        for (int i = 0; i < node.choices.Count; i++)
        {
            var choice = node.choices[i];
            var ui = EventNodeOptions[i];

            ui.EventSelectionButton.gameObject.SetActive(true);
            ui.EventSelectionButtonText.text = choice.buttonText;

            ui.EventSelectionButton.onClick.RemoveAllListeners();

            ui.EventSelectionButton.onClick.AddListener(() =>
            {
                foreach (var effect in choice.effects)
                    effect.Execute();

                EventNode nextNode = CurrentEvent.GetNode(choice.nextNodeID);

                if (nextNode != null)
                    LoadEventNode(nextNode);
            });
        }
    }

    #endregion
}
