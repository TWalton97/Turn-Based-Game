using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Units", menuName = "Unit Data")]
public class UnitDataSO : ScriptableObject
{
    public string UnitName;
    public int MaxHealth;
    public List<BaseAbility> Abilities;
    public GameObject UnitModel;
}
