using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ItemSO : ScriptableObject
{
    public string ItemName;
    [TextArea] public string Description;
    [TextArea] public string ItemInformation;

    public int GoldValue;
    public bool Stackable = true;

    protected virtual void OnEnable()
    {
        ItemDatabase.RegisterItem(this);
    }
}
