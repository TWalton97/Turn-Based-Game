using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "Status Effects", menuName = "StatusEffects/Damage Status Effect")]
public class DamageStatusEffect : StatusEffect
{
    public DamageType DamageType;

    public override void ServerExecuteEffect(UnitController controller, int statusEffectPower = 1)
    {
        HitResult hitResult = new HitResult();
        hitResult.Blocked = false;
        hitResult.Crit = false;
        hitResult.Dodged = false;
        hitResult.DamageType = DamageType;
        hitResult.Damage = statusEffectPower;

        controller.ServerTakeDamage(hitResult);
    }

    public override void ClientExecuteEffect(UnitController controller, int statusEffectPower = 1)
    {
        HitResult hitResult = new HitResult();
        hitResult.Blocked = false;
        hitResult.Crit = false;
        hitResult.Dodged = false;
        hitResult.DamageType = DamageType;
        hitResult.Damage = statusEffectPower;

        controller.ClientTakeDamage(hitResult);
    }

    public override string ConstructDescriptionString(StatusEffectInstance statusEffectInstance)
    {
        return StringBuilderUtilities.Build(StatusDescription, statusEffectInstance.StatusEffectPower);
    }
}
