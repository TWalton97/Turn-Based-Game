using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatusEffectInstance
{
    public StatusEffect StatusEffect;
    public UnitController UnitController;

    public int RemainingNumberOfTurns;
    public float StatusEffectPower;

    public bool isServerExpired = false;
    public bool isAppliedOnClient = false;

    public bool clientOnApplicationCompleted = false;

    public string statusEffectId;

    public StatusEffectInstance(UnitController _unitControler, StatusEffect _statusEffect, int _numberOfTurns, float _statusEffectPower, string _statusEffectId)
    {
        UnitController = _unitControler;
        StatusEffect = _statusEffect;
        RemainingNumberOfTurns = _numberOfTurns;
        StatusEffectPower = _statusEffectPower;
        statusEffectId = _statusEffectId;
        isAppliedOnClient = false;
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
