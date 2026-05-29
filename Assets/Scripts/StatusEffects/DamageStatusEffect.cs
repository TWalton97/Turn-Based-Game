using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "Status Effects", menuName = "StatusEffects/Damage Status Effect")]
public class DamageStatusEffect : StatusEffect
{
    public DamageType DamageType;
    public int DamageAmount;

    public override void ServerExecuteEffect(UnitController controller)
    {
        HitResult hitResult = new HitResult();
        hitResult.Blocked = false;
        hitResult.Crit = false;
        hitResult.Dodged = false;
        hitResult.DamageType = DamageType;
        hitResult.Damage = DamageAmount;

        controller.ServerTakeDamage(hitResult);
    }

    public override void ClientExecuteEffect(UnitController controller)
    {
        HitResult hitResult = new HitResult();
        hitResult.Blocked = false;
        hitResult.Crit = false;
        hitResult.Dodged = false;
        hitResult.DamageType = DamageType;
        hitResult.Damage = DamageAmount;

        controller.ClientTakeDamage(hitResult);
    }
}
