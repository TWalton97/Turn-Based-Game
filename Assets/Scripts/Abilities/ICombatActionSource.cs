using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICombatActionSource
{
    bool CanUse(UnitController controller);

    void ConsumeCost(UnitController controller);
}
