using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    //This script controls spawning of players and units

    public static SpawnManager instance;

    public UnitController GoblinPrefab;

    public void SpawnUnit(UnitController unit)
    {
        BattleSlot slot = BattleManager.instance.ReturnEmptyBattleSlotOfType(unit.UnitTeam);
        UnitController controller = Instantiate(GoblinPrefab, slot.UnitHolder);
        BattleManager.instance.RegisterUnit(controller);
        slot.BindUnitToSlot(controller);
    }

    [ContextMenu("Spawn Goblin")]
    public void SpawnGoblin()
    {
        SpawnUnit(GoblinPrefab);
    }
}
