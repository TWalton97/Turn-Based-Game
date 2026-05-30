using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Equipment Item")]
public class EquipmentItemSO : ItemSO
{
    public EquipmentSlot EquipmentSlot;

    public List<StatModifier> statModifiers;
}

[System.Serializable]
public class StatModifier
{
    public StatType stat;
    public float value;

    public string sourceId;
}

public enum EquipmentSlot
{
    Helmet,
    Chestpiece,
    Leggings,
    Boots,
    Weapon,
    Charm,
    Consumable,
}