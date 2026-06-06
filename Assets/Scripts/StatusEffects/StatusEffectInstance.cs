using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[Serializable]
public class StatusEffectInstance
{
    public StatusEffect StatusEffect;
    public UnitController UnitController;
    public UnitController SourceController;

    public int RemainingNumberOfTurns;
    public float StatusEffectPower;

    public bool isServerExpired = false;
    public bool isAppliedOnClient = false;

    public bool clientOnApplicationCompleted = false;

    public FixedString64Bytes statusEffectId;

    public StatusEffectInstance(UnitController _unitController, UnitController _sourceController, StatusEffect _statusEffect, int _numberOfTurns, float _statusEffectPower, FixedString64Bytes _statusEffectId)
    {
        UnitController = _unitController;
        SourceController = _sourceController;
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
