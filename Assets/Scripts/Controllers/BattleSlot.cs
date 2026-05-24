using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSlot : MonoBehaviour
{
    public Team Team;
    public UnitController UnitController;
    public Transform UnitHolder;

    public void BindUnitToSlot(UnitController controller)
    {
        UnitController = controller;

        UnitController.OnDie += () => UnbindUnit();
    }

    public void UnbindUnit()
    {
        UnitController.OnDie -= () => UnbindUnit();

        UnitController = null;
    }

}
