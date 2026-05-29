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

        UnitController.OnClientDie += () => UnbindUnit();
    }

    public void UnbindUnit()
    {
        UnitController.OnClientDie -= () => UnbindUnit();

        UnitController = null;
    }

}
