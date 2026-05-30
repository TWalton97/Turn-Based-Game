using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatusEffectController : MonoBehaviour
{
    private UnitController unitController;
    public List<StatusEffectInstance> ActiveStatusEffects = new();
    public List<string> StatusEffectNames;

    public Action ServerOnStatusEffectsProcced;
    public Action ClientOnStatusEffectsProcced;

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    public void ServerProcStatusEffects(ActivationTime activationTime)
    {
        List<StatusEffectInstance> statusEffectInstances = ActiveStatusEffects.Where(t => t.StatusEffect.ActivationTime == activationTime).ToList();
        foreach (StatusEffectInstance statusEffectInstance in statusEffectInstances)
        {
            statusEffectInstance.ServerActivateStatusEffect(unitController);
        }
        ServerOnStatusEffectsProcced?.Invoke();
    }

    public IEnumerator ClientProcStatusEffects(ActivationTime activationTime)
    {
        List<StatusEffectInstance> statusEffectInstances = ActiveStatusEffects.Where(t => t.StatusEffect.ActivationTime == activationTime).ToList();
        foreach (StatusEffectInstance statusEffectInstance in statusEffectInstances)
        {
            statusEffectInstance.ClientActivateStatusEffect(unitController);
            if (statusEffectInstance.RemainingNumberOfTurns <= 0)
                RemoveStatusEffect(statusEffectInstance.StatusEffect);
            yield return new WaitForSeconds(0.7f);
        }
        ClientOnStatusEffectsProcced?.Invoke();
    }

    public void AddStatusEffect(StatusEffect statusEffect, int statusEffectPower = 1)
    {
        StatusEffectInstance existingStatusEffectInstance = ActiveStatusEffects.Find(t => t.StatusEffect == statusEffect);
        if (existingStatusEffectInstance != null)
        {
            existingStatusEffectInstance.RemainingNumberOfTurns = statusEffect.NumberOfTurns;
            existingStatusEffectInstance.StatusEffectPower = Mathf.Max(existingStatusEffectInstance.StatusEffectPower, statusEffectPower);
            return;
        }

        ActiveStatusEffects.Add(new StatusEffectInstance(unitController, statusEffect, statusEffect.NumberOfTurns, statusEffectPower));
        StatusEffectNames.Add(statusEffect.StatusEffectName);
    }

    public void RemoveStatusEffect(StatusEffect statusEffect)
    {
        if (!ActiveStatusEffects.Where(t => t.StatusEffect == statusEffect).Any())
            return;

        ActiveStatusEffects.Remove(ActiveStatusEffects.Where(t => t.StatusEffect == statusEffect).First());
        StatusEffectNames.Remove(statusEffect.StatusEffectName);
    }

    public List<StatusEffectInstance> ReturnStatusEffectInstancesOfType(BuffType buffType)
    {
        List<StatusEffectInstance> statusEffectInstances = ActiveStatusEffects.Where(t => t.StatusEffect.BuffType == buffType).ToList();
        return statusEffectInstances;
    }
}
