using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EventNode
{
    public string nodeID;
    public Sprite image;
    [TextArea] public string description;

    public List<EventChoice> choices;
}
