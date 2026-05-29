using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatusEffectController : MonoBehaviour
{
    private UnitController unitController;
    public List<StatusEffect> ActiveStatusEffects = new();
    public List<string> StatusEffectNames;

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    public void ServerProcStatusEffects(ActivationTime activationTime)
    {
        List<StatusEffect> statusEffects = ActiveStatusEffects.Where(t => t.ActivationTime == activationTime).ToList();
        foreach (StatusEffect statusEffect in statusEffects)
        {
            statusEffect.ServerExecuteEffect(unitController);
        }
    }

    public IEnumerator ClientProcStatusEffects(ActivationTime activationTime)
    {
        List<StatusEffect> statusEffects = ActiveStatusEffects.Where(t => t.ActivationTime == activationTime).ToList();
        foreach (StatusEffect statusEffect in statusEffects)
        {
            statusEffect.ClientExecuteEffect(unitController);
            yield return new WaitForSeconds(1f);
        }
    }

    public void AddStatusEffect(StatusEffect statusEffect)
    {
        ActiveStatusEffects.Add(statusEffect);
        StatusEffectNames.Add(statusEffect.StatusEffectName);
    }

    public void RemoveStatusEffect(StatusEffect statusEffect)
    {
        if (!ActiveStatusEffects.Contains(statusEffect))
            return;

        ActiveStatusEffects.Remove(statusEffect);
        StatusEffectNames.Remove(statusEffect.StatusEffectName);
    }

    public List<StatusEffect> ReturnStatusEffectsOfType(BuffType buffType)
    {
        List<StatusEffect> statusEffects = ActiveStatusEffects.Where(t => t.BuffType == buffType).ToList();
        return statusEffects;
    }
}
