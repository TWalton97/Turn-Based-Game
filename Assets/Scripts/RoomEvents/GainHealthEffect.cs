using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Events", menuName = "Events/EventEffect/GainHealthEffect")]
public class GainHealthEffect : EventEffect
{
    public int HealthAmount;
    public override void Execute()
    {
        foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
        {
            controller.ServerHeal(HealthAmount);
        }
    }
}
