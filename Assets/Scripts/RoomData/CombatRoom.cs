using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Combat Room", menuName = "Room Data/Combat Room")]
public class CombatRoom : RoomData
{
    public GameObject RoomBackground;
    public List<UnitController> Enemies;
}
