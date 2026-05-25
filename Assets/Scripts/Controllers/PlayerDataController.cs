using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDataController : MonoBehaviour
{
    //PlayerDataController is an extra script that goes onto player-controlled UnitControllers
    //They store 3 lists of data
    //Inventory -> Items the player has (ingredients, equipment, consumables)
    //Equipment -> Items the player currently has equipped
    //Stats -> The player's current stats, keeps track of attributes, exp, and stat points

    //We get the class preset from the UnitController
    private UnitController UnitController;
    public ClassStatPresetSO ClassPresetSO;
    public PlayerStats PlayerStats;

    void Awake()
    {
        UnitController = GetComponent<UnitController>();
        ClassPresetSO = UnitController.UnitData;
        AssignStatsFromClassPreset();
    }

    private void AssignStatsFromClassPreset()
    {
        PlayerStats.Level = 1;
        PlayerStats.CurrentExp = 0;
        PlayerStats.AvailableStatPoints = 0;
        PlayerStats.Gold = 0;

        // Attributes
        PlayerStats.Strength = ClassPresetSO.Strength;
        PlayerStats.Dexterity = ClassPresetSO.Dexterity;
        PlayerStats.Constitution = ClassPresetSO.Constitution;
        PlayerStats.Intelligence = ClassPresetSO.Intelligence;
        PlayerStats.Faith = ClassPresetSO.Faith;
        PlayerStats.Charisma = ClassPresetSO.Charisma;
        PlayerStats.Luck = ClassPresetSO.Luck;

        // Combat stats
        PlayerStats.InitiativeMin = ClassPresetSO.InitiativeMin;
        PlayerStats.InitiativeMax = ClassPresetSO.InitiativeMax;
        PlayerStats.CritChance = ClassPresetSO.CritChance;
        PlayerStats.CritDamage = ClassPresetSO.CritDamage;
        PlayerStats.BlockChance = ClassPresetSO.BlockChance;
        PlayerStats.BlockDamageReduction = ClassPresetSO.BlockDamageReduction;
        PlayerStats.DodgeChance = ClassPresetSO.DodgeChance;
        PlayerStats.Aggro = ClassPresetSO.Aggro;
        PlayerStats.Lifesteal = ClassPresetSO.Lifesteal;
        PlayerStats.EnergyGain = ClassPresetSO.EnergyGain;
        PlayerStats.LuckyDrop = ClassPresetSO.LuckyDrop;
        PlayerStats.IncomingHealing = ClassPresetSO.IncomingHealing;
        PlayerStats.OutgoingHealing = ClassPresetSO.OutgoingHealing;
    }

    public void AddExp(int amount)
    {
        PlayerStats.CurrentExp += amount;
        if (PlayerStats.CurrentExp >= ExperienceValues.ExpToNextLevel[PlayerStats.Level])
        {
            PlayerStats.CurrentExp -= ExperienceValues.ExpToNextLevel[PlayerStats.Level];
            PlayerStats.Level += 1;
            PlayerStats.AvailableStatPoints += 3;
            AddExp(0);
        }
    }
}

[System.Serializable]
public class PlayerStats
{
    public int Level;
    public int CurrentExp;
    public int AvailableStatPoints;
    public int Gold;

    //Attributes
    public int Strength;
    public int Dexterity;
    public int Constitution;
    public int Intelligence;
    public int Faith;
    public int Charisma;
    public int Luck;

    //Combat stats
    public int InitiativeMin;
    public int InitiativeMax;
    public float CritChance;
    public float CritDamage;
    public float BlockChance;
    public float BlockDamageReduction;
    public int DodgeChance;
    public float Aggro;
    public float Lifesteal;
    public float EnergyGain;
    public int LuckyDrop;
    public float IncomingHealing;
    public float OutgoingHealing;
}

public static class ExperienceValues
{
    public static List<int> ExpToNextLevel = new()
    {
        0, 15, 25, 45, 70, 100,
    };
}