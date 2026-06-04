using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Combat Room", menuName = "Room Data/Combat Room")]
public class CombatRoom : RoomData
{
    public int presetId;
    public GameObject RoomBackground;
    public List<CombatRoomEnemyEntry> Enemies;
}

[System.Serializable]
public class CombatRoomEnemyEntry
{
    public UnitController Unit;
    public int Level;
}
