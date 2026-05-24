using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Events", menuName = "Events/EventNode")]
public class EventDefinition : ScriptableObject
{
    public string eventID;
    public string startNodeID;

    public List<EventNode> nodes = new();

    public EventNode GetNode(string nodeID)
    {
        return nodes.Find(n => n.nodeID == nodeID);
    }
}
