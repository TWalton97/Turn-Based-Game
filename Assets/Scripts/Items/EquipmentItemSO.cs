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
    public Attribute attribute;
    public float value;
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