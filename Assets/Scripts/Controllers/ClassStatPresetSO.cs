using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Classes", menuName = "Classes/Class Stat Preset")]
public class ClassStatPresetSO : ScriptableObject
{
    public string ClassName;
    public int MaxMana = 5;

    public List<AbilityUnlock> AbilityUnlocks;

    public int Strength;
    public int Dexterity;
    public int Intelligence;
    public int Constitution;
}
