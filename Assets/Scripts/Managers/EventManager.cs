using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventManager : MonoBehaviour
{
    //Stores a list of all possible events
    //Whenever a function gets called, it will pull 3 possible events and display them
    //Once an event is chosen, either the event manager displays the new menus or the progression manager loads a combat sequence
    //Chosen events are also removed from the list of possible events

    public static EventManager instance;

    public List<EventDefinition> Events;

    //Event selection
    public GameObject EventSelectionMenu;
    [System.Serializable]
    public class EventSelection
    {
        public Image EventSelectionImage;
        public Button EventSelectionButton;
        public TextMeshProUGUI EventSelectionButtonText;
    }
    public List<EventSelection> EventSelections = new();

    //Event nodes
    public GameObject StoryEventMenu;
    public Image EventNodeImage;
    public TextMeshProUGUI EventNodeName;
    public TextMeshProUGUI EventNodeDescription;
    public List<EventSelection> EventNodeOptions;

    public EventDefinition CurrentEvent;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void PopulateEventOptions()
    {
        EventSelectionMenu.SetActive(true);
        StoryEventMenu.SetActive(false);

        for (int i = 0; i < 3; i++)
        {
            EventDefinition selectedEvent = Events[Random.Range(0, Events.Count)];
            EventSelections[i].EventSelectionImage.sprite = selectedEvent.GetNode(selectedEvent.startNodeID).image;
            EventSelections[i].EventSelectionButtonText.text = selectedEvent.eventID;

            EventNode capturedNode = selectedEvent.GetNode(selectedEvent.startNodeID);

            EventSelections[i].EventSelectionButton.onClick.RemoveAllListeners();

            EventSelections[i].EventSelectionButton.onClick.AddListener(() =>
            {
                LoadEventNode(capturedNode);
                CurrentEvent = selectedEvent;
            });
        }
    }

    public void LoadEventNode(EventNode node)
    {
        EventSelectionMenu.SetActive(false);
        StoryEventMenu.SetActive(true);

        EventNodeImage.sprite = node.image;
        EventNodeName.text = "Event";
        EventNodeDescription.text = node.description;

        foreach (EventSelection eventSelection in EventNodeOptions)
        {
            eventSelection.EventSelectionButton.gameObject.SetActive(false);
        }

        for (int i = 0; i < node.choices.Count; i++)
        {
            EventChoice choice = node.choices[i];

            EventNodeOptions[i].EventSelectionButton.gameObject.SetActive(true);
            EventNodeOptions[i].EventSelectionButtonText.text = node.choices[i].buttonText;
            EventNodeOptions[i].EventSelectionButton.onClick.RemoveAllListeners();
            EventNodeOptions[i].EventSelectionButton.onClick.AddListener(() =>
            {
                foreach (EventEffect effect in choice.effects)
                    effect.Execute();

                EventNode nextNode = CurrentEvent.GetNode(choice.nextNodeID);
                if (nextNode != null)
                    LoadEventNode(nextNode);
            });
        }
    }
}
