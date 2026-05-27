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

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    #region EVENT GENERATION

    //Event manager timeline

    //On event started, the server chooses 3 events from the pool and distributes their indexes to each player
    //Each player takes those indexes and populates the event choices
    //Each event choice button submits a vote for that event's index to the server

    //Once the number of votes for an event >= number of clients, the server tells all clients to load the starting node of that event
    //The starting node populates the buttons with more vote buttons based on the option index
    //Clicking a button submits a vote for that index

    //General loop: Clients submit votes to server, server waits until votes reach player count, server tells clients what outcome is

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
        ui.EventSelectionButtonText.text = eventDef.eventID;

        ui.EventSelectionButton.onClick.RemoveAllListeners();

        string eventID = eventDef.eventID; // IMPORTANT: stable identifier

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
        Debug.Log($"Submitting vote to server for option {optionIndex}");   //This could be any option - event selection, event option selection, etc...

        ulong senderId = rpcParams.Receive.SenderClientId;

        PlayerVotes[senderId] = optionIndex;

        CheckVotes();
    }

    private void CheckVotes()
    {
        Dictionary<int, int> voteCounts = new();

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

    private void ResolveWinningVote(int winningEventIndex)
    {
        if (CurrentEvent == null)
        {
            Debug.Log($"Event {winningEventIndex} had the most votes, loading event {winningEventIndex}");
            //If there is no current event, then we're voting on an event to start
            //We set the winning event to the selectedEventOptions of the correct index
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
            Debug.Log($"Event option {winningEventIndex} had the most votes, executing event option {winningEventIndex}");
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
        Debug.Log($"Executed all event effects for option {currentEventOptionIndex}");
        
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

            int capturedChoiceIndex = i;

            //We need to just change this to submit a vote with an index and let the server decide
            ui.EventSelectionButton.onClick.AddListener(() =>
            {
                SubmitEventVoteServerRpc(capturedChoiceIndex);
                // foreach (var effect in choice.effects)
                //     effect.Execute();

                // EventNode nextNode = CurrentEvent.GetNode(choice.nextNodeID);

                // if (nextNode != null)
                //     LoadEventNode(nextNode);
            });
        }
    }

    #endregion
}
