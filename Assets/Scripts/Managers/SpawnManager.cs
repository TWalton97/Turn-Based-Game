using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    //This script controls spawning of players and units

    public static SpawnManager instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void SpawnUnit(UnitController unit)
    {
        BattleSlot slot = BattleManager.instance.ReturnEmptyBattleSlotOfType(unit.UnitTeam);
        UnitController controller = Instantiate(unit, slot.UnitHolder);
        BattleManager.instance.RegisterUnit(controller);
        slot.BindUnitToSlot(controller);
    }
}
