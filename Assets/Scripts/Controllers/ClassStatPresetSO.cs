using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Classes", menuName = "Classes/Class Stat Preset")]
public class ClassStatPresetSO : ScriptableObject
{
    public string ClassName;
    public int MaxHealth;
    public int MaxMana = 5;

    public int Strength;
    public int Dexterity;
    public int Constitution;
    public int Intelligence;
    public int Faith;
    public int Charisma;
    public int Luck;

    //Combat stats
    public int InitiativeMin = 3;
    public int InitiativeMax = 6;
    public float CritChance;
    public float CritDamage = 100;
    public float BlockChance;
    public float BlockDamageReduction = 50;
    public int DodgeChance;
    public float Aggro;
    public float Lifesteal;
    public float EnergyGain;
    public int LuckyDrop;
    public float IncomingHealing = 100;
    public float OutgoingHealing = 100;
}
