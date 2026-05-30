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

    public Action OnStatusEffectsChanged;

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    public void ServerProcStatusEffects(ActivationTime activationTime)
    {
        List<StatusEffectInstance> statusEffectInstances = ActiveStatusEffects.Where(t => t.StatusEffect.ActivationTime == activationTime).ToList();
        foreach (StatusEffectInstance statusEffectInstance in statusEffectInstances)
        {
            statusEffectInstance.ServerExecuteEffect(unitController);
        }
        ServerOnStatusEffectsProcced?.Invoke();
    }

    public IEnumerator ClientProcStatusEffects(ActivationTime activationTime)
    {
        List<StatusEffectInstance> statusEffectInstances = ActiveStatusEffects.Where(t => t.StatusEffect.ActivationTime == activationTime).ToList();
        foreach (StatusEffectInstance statusEffectInstance in statusEffectInstances)
        {
            statusEffectInstance.ClientExecuteEffect(unitController);
            yield return new WaitForSeconds(0.7f);
        }
        ClientOnStatusEffectsProcced?.Invoke();
    }

    public void ReduceRemainingTurnTimer()
    {
        for (int i = ActiveStatusEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffects[i].RemainingNumberOfTurns--;

            if (ActiveStatusEffects[i].RemainingNumberOfTurns <= 0)
            {
                RemoveStatusEffect(ActiveStatusEffects[i].StatusEffect);
            }
        }
    }

    public void AddStatusEffect(StatusEffect statusEffect, float statusEffectPower = 1)
    {
        StatusEffectInstance existingStatusEffectInstance = ActiveStatusEffects.Find(t => t.StatusEffect == statusEffect);
        if (existingStatusEffectInstance != null)
        {
            existingStatusEffectInstance.RemainingNumberOfTurns = statusEffect.NumberOfTurns;
            existingStatusEffectInstance.StatusEffectPower = Mathf.Max(existingStatusEffectInstance.StatusEffectPower, statusEffectPower);
            OnStatusEffectsChanged?.Invoke();
            return;
        }

        StatusEffectInstance statusEffectInstance = new StatusEffectInstance(unitController, statusEffect, statusEffect.NumberOfTurns, statusEffectPower);

        statusEffect.ServerOnApplication(unitController, statusEffectInstance);
        
        if (statusEffect.NumberOfTurns == 0)
            return;

        ActiveStatusEffects.Add(statusEffectInstance);
        StatusEffectNames.Add(statusEffect.StatusEffectName);

        OnStatusEffectsChanged?.Invoke();
    }

    public void RemoveStatusEffect(StatusEffect statusEffect)
    {
        if (!ActiveStatusEffects.Where(t => t.StatusEffect == statusEffect).Any())
            return;

        foreach (StatusEffectInstance instance in ActiveStatusEffects.Where(t => t.StatusEffect == statusEffect))
        {
            instance.StatusEffect.ServerRemoveStatus(unitController, instance);
        }

        ActiveStatusEffects.Remove(ActiveStatusEffects.Where(t => t.StatusEffect == statusEffect).First());
        StatusEffectNames.Remove(statusEffect.StatusEffectName);

        OnStatusEffectsChanged?.Invoke();
    }

    public List<StatusEffectInstance> ReturnStatusEffectInstancesOfType(BuffType buffType)
    {
        List<StatusEffectInstance> statusEffectInstances = ActiveStatusEffects.Where(t => t.StatusEffect.BuffType == buffType).ToList();
        return statusEffectInstances;
    }
}
