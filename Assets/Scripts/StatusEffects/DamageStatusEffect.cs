using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "Status Effects", menuName = "StatusEffects/Damage Status Effect")]
public class DamageStatusEffect : StatusEffect
{
    public DamageType DamageType;

    public override void ServerOnApplication(UnitController controller, StatusEffectInstance instance)
    {

    }

    public override void ClientOnApplication(UnitController controller, StatusEffectInstance instance)
    {

    }
    public override void ServerExecuteEffect(UnitController controller, StatusEffectInstance instance)
    {
        HitResult hitResult = new HitResult();
        hitResult.Blocked = false;
        hitResult.Crit = false;
        hitResult.Dodged = false;
        hitResult.DamageType = DamageType;
        hitResult.Damage = instance.StatusEffectPower;

        controller.ServerTakeDamage(hitResult);
    }

    public override void ClientExecuteEffect(UnitController controller, StatusEffectInstance instance)
    {
        HitResult hitResult = new HitResult();
        hitResult.Blocked = false;
        hitResult.Crit = false;
        hitResult.Dodged = false;
        hitResult.DamageType = DamageType;
        hitResult.Damage = instance.StatusEffectPower;

        controller.ClientTakeDamage(hitResult);
    }

    public override string ConstructDescriptionString(StatusEffectInstance statusEffectInstance)
    {
        return StringBuilderUtilities.Build(StatusDescription, statusEffectInstance.StatusEffectPower);
    }

    public override void ServerRemoveStatus(UnitController controller, StatusEffectInstance instance)
    {

    }
}
