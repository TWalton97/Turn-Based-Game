using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class StatusEffectController : MonoBehaviour
{
    private UnitController unitController;
    public List<StatusEffectInstance> ActiveStatusEffects = new();

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
            if (statusEffectInstance.isServerExpired)
                return;

            statusEffectInstance.ServerExecuteEffect(unitController);
        }
        ServerOnStatusEffectsProcced?.Invoke();
    }

    public IEnumerator ClientProcStatusEffects(ActivationTime activationTime)
    {
        List<StatusEffectInstance> statusEffectInstances = ActiveStatusEffects.Where(t => t.StatusEffect.ActivationTime == activationTime).ToList();
        foreach (StatusEffectInstance statusEffectInstance in statusEffectInstances)
        {
            if (!statusEffectInstance.clientOnApplicationCompleted)
            {
                statusEffectInstance.clientOnApplicationCompleted = true;
                statusEffectInstance.StatusEffect.ClientOnApplication(unitController, statusEffectInstance);
            }

            statusEffectInstance.ClientExecuteEffect(unitController);
            yield return new WaitForSeconds(0.7f);
        }
        ClientOnStatusEffectsProcced?.Invoke();
    }

    public void ServerReduceRemainingTurnTimer()
    {
        for (int i = ActiveStatusEffects.Count - 1; i >= 0; i--)
        {
            if (ActiveStatusEffects[i].RemainingNumberOfTurns - 1 <= 0)
            {
                if (ActiveStatusEffects[i].StatusEffect.ActivationTime == ActivationTime.OnExpire)
                {
                    ActiveStatusEffects[i].ServerExecuteEffect(unitController);
                }

                ActiveStatusEffects[i].isServerExpired = true;
            }
        }
    }

    public void ClientReduceRemainingTurnTimer()
    {
        for (int i = ActiveStatusEffects.Count - 1; i >= 0; i--)
        {
            if (!ActiveStatusEffects[i].isAppliedOnClient)
                continue;
            ActiveStatusEffects[i].RemainingNumberOfTurns--;

            if (ActiveStatusEffects[i].RemainingNumberOfTurns <= 0)
            {
                if (ActiveStatusEffects[i].StatusEffect.ActivationTime == ActivationTime.OnExpire)
                {
                    ActiveStatusEffects[i].ClientExecuteEffect(unitController);
                }

                RemoveStatusEffect(ActiveStatusEffects[i].StatusEffect);
            }
        }
        OnStatusEffectsChanged?.Invoke();
    }

    public void AddStatusEffect(StatusEffect statusEffect, FixedString64Bytes statusEffectId, float statusEffectPower = 1)
    {
        StatusEffectInstance existingStatusEffectInstance = ActiveStatusEffects.Find(t => t.StatusEffect == statusEffect);
        if (existingStatusEffectInstance != null)
        {
            existingStatusEffectInstance.RemainingNumberOfTurns = statusEffect.NumberOfTurns;
            existingStatusEffectInstance.StatusEffectPower = Mathf.Max(existingStatusEffectInstance.StatusEffectPower, statusEffectPower);
            OnStatusEffectsChanged?.Invoke();
            return;
        }

        StatusEffectInstance statusEffectInstance = new StatusEffectInstance(unitController, statusEffect, statusEffect.NumberOfTurns, statusEffectPower, statusEffectId);

        statusEffect.ServerOnApplication(unitController, statusEffectInstance);

        if (statusEffectInstance.RemainingNumberOfTurns == 0)
            statusEffectInstance.isServerExpired = true;

        ActiveStatusEffects.Add(statusEffectInstance);

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

        OnStatusEffectsChanged?.Invoke();
    }

    public List<StatusEffectInstance> ReturnStatusEffectInstancesOfType(BuffType buffType)
    {
        List<StatusEffectInstance> statusEffectInstances = ActiveStatusEffects.Where(t => t.StatusEffect.BuffType == buffType).ToList();
        return statusEffectInstances;
    }
}
