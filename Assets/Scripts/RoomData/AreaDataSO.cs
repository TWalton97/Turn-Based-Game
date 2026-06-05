using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AreaData", menuName = "AreaData")]
public class AreaDataSO : ScriptableObject
{
    public string AreaName;                         //The name of the area
    public int NumberOfRooms;                       //How many rooms before the boss
    public List<UnitController> AvailableEnemies;   //All of the enemies that can random appear in this area
    public GameObject DefaultAreaBackground;        //The background we use for random encounters
    public RoomData BossRoomData;                   //The boss room data
}
