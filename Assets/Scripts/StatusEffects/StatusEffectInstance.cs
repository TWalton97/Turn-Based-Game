using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectInstance
{
    public StatusEffect StatusEffect;
    public UnitController UnitController;

    public int RemainingNumberOfTurns;
    public int StatusEffectPower;

    public StatusEffectInstance(UnitController _unitControler, StatusEffect _statusEffect, int _numberOfTurns, int _statusEffectPower)
    {
        UnitController = _unitControler;
        StatusEffect = _statusEffect;
        RemainingNumberOfTurns = _numberOfTurns;
        StatusEffectPower = _statusEffectPower;
    }

    public void ServerActivateStatusEffect(UnitController controller)
    {
        StatusEffect.ServerExecuteEffect(controller, StatusEffectPower);
    }

    public void ClientActivateStatusEffect(UnitController controller)
    {
        StatusEffect.ClientExecuteEffect(controller, StatusEffectPower);
        RemainingNumberOfTurns--;
    }
}
