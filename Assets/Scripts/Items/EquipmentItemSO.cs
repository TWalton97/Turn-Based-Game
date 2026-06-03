using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Equipment Item")]
public class EquipmentItemSO : ItemSO
{
    public EquipmentSlot EquipmentSlot;

    public List<StatModifier> statModifiers;
}

[Serializable]
public struct StatModifier : INetworkSerializable, IEquatable<StatModifier>
{
    public StatType stat;
    public float value;

    public FixedString64Bytes sourceId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out stat);
            reader.ReadValueSafe(out value);
            reader.ReadValueSafe(out sourceId);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(stat);
            writer.WriteValueSafe(value);
            writer.WriteValueSafe(sourceId);
        }
    }

    public bool Equals(StatModifier other)
    {
        return stat == other.stat && value == other.value && sourceId == other.sourceId;
    }
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