using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectInstance
{
    public StatusEffect StatusEffect;
    public UnitController UnitController;

    public int RemainingNumberOfTurns;

    public StatusEffectInstance(UnitController _unitControler, StatusEffect _statusEffect, int _numberOfTurns)
    {
        UnitController = _unitControler;
        StatusEffect = _statusEffect;
        RemainingNumberOfTurns = _numberOfTurns;
    }

    public void ServerActivateStatusEffect(UnitController controller)
    {
        StatusEffect.ServerExecuteEffect(controller);
    }

    public void ClientActivateStatusEffect(UnitController controller)
    {
        StatusEffect.ClientExecuteEffect(controller);
        RemainingNumberOfTurns--;
    }
}
