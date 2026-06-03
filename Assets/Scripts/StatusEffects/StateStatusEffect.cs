using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Status Effects", menuName = "StatusEffects/State Status Effect")]
public class StateStatusEffect : StatusEffect
{
    public List<UnitStateTags> StateTags;

    public override void ServerOnApplication(UnitController controller, StatusEffectInstance instance)
    {
        foreach (UnitStateTags tag in StateTags)
        {
            controller.StateModifiers.Add(new StateModifier
            {
                stateTag = tag,
                sourceId = instance.statusEffectId,
            });
        }
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
        controller.StateModifiers.RemoveAll(t => t.sourceId == instance.statusEffectId);
    }

    public override string ConstructDescriptionString(StatusEffectInstance statusEffectInstance)
    {
        return StringBuilderUtilities.Build(StatusDescription, statusEffectInstance.StatusEffectPower);
    }
}

[System.Serializable]
public class StateModifier
{
    public UnitStateTags stateTag;
    public FixedString64Bytes sourceId;
}
