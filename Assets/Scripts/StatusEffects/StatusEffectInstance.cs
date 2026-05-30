using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectInstance
{
    public StatusEffect StatusEffect;
    public UnitController UnitController;

    public int RemainingNumberOfTurns;
    public float StatusEffectPower;

    public string id;

    public StatusEffectInstance(UnitController _unitControler, StatusEffect _statusEffect, int _numberOfTurns, float _statusEffectPower)
    {
        UnitController = _unitControler;
        StatusEffect = _statusEffect;
        RemainingNumberOfTurns = _numberOfTurns;
        StatusEffectPower = _statusEffectPower;
        id = Guid.NewGuid().ToString();
    }

    public void ServerExecuteEffect(UnitController controller)
    {
        StatusEffect.ServerExecuteEffect(controller, this);
    }

    public void ClientExecuteEffect(UnitController controller)
    {
        StatusEffect.ClientExecuteEffect(controller, this);
    }
}
