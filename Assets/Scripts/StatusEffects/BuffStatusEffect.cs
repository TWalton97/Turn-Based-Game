using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Status Effects", menuName = "StatusEffects/Buff Status Effect")]
public class BuffStatusEffect : StatusEffect
{
    public List<StatModifier> StatModifiers;

    public override void ServerOnApplication(UnitController controller, StatusEffectInstance instance)
    {
        foreach (StatModifier mod in StatModifiers)
        {
            controller.StatModifiers.Add(new StatModifier
            {
                stat = mod.stat,
                value = mod.value,
                sourceId = instance.id
            });
            instance.StatusEffectPower = mod.value;
        }
        controller.CachedStatsDirty = true;
    }

    public override void ClientOnApplication(UnitController controller, StatusEffectInstance instance)
    {
        
    }

    public override void ServerExecuteEffect(UnitController controller, StatusEffectInstance instance)
    {
        
    }

    public override void ClientExecuteEffect(UnitController controller, StatusEffectInstance instance)
    {
        
    }

    public override void ServerRemoveStatus(UnitController controller, StatusEffectInstance instance)
    {
        controller.StatModifiers.RemoveAll(t => t.sourceId == instance.id);
        controller.CachedStatsDirty = true;
    }

    public override string ConstructDescriptionString(StatusEffectInstance statusEffectInstance)
    {
        return StringBuilderUtilities.Build(StatusDescription, statusEffectInstance.StatusEffectPower);
    }
}
