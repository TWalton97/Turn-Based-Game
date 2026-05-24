using UnityEngine;

[CreateAssetMenu(fileName = "Events", menuName = "Events/EventNode")]
public class EventDefinition : ScriptableObject
{
    public string eventID;
    public EventNode startNode;
}
