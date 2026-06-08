using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "Status Effects", menuName = "StatusEffects/Damage Status Effect")]
public class DamageStatusEffect : StatusEffect
{
    public DamageType DamageType;
    public List<AbilityEffectDamageScaling> DamageScaling;

    public override void ServerOnApplication(UnitController controller, StatusEffectInstance instance)
    {

    }

    public override void ClientOnApplication(UnitController controller, StatusEffectInstance instance)
    {

    }
    public override void ServerExecuteEffect(UnitController controller, StatusEffectInstance instance)
    {
        HitResult hitResult = new HitResult();
        hitResult.DamageSource = DamageSource.StatusEffect;
        hitResult.Blocked = false;
        hitResult.Crit = false;
        hitResult.Dodged = false;
        hitResult.DamageType = DamageType;
        hitResult.Damage = CalculateDamage(instance);

        controller.ServerTakeDamage(hitResult);
    }

    public override void ClientExecuteEffect(UnitController controller, StatusEffectInstance instance)
    {
        HitResult hitResult = new HitResult();
        hitResult.DamageSource = DamageSource.StatusEffect;
        hitResult.Blocked = false;
        hitResult.Crit = false;
        hitResult.Dodged = false;
        hitResult.DamageType = DamageType;
        hitResult.Damage = CalculateDamage(instance);

        controller.ClientTakeDamage(hitResult);
    }

    public override string ConstructDescriptionString(StatusEffectInstance statusEffectInstance)
    {
        return StringBuilderUtilities.Build(StatusDescription, statusEffectInstance.StatusEffectPower);
    }

    public override void ServerRemoveStatus(UnitController controller, StatusEffectInstance instance)
    {

    }

    private float CalculateDamage(StatusEffectInstance instance)
    {
        UnitController source = instance.SourceController;
        DamageStatusEffect status = instance.StatusEffect as DamageStatusEffect;

        float damage = 0;
        for (int i = 0; i < status.DamageScaling.Count; i++)
        {
            switch (status.DamageScaling[i].Attribute)
            {
                case StatType.STR:
                    damage += source.GetStatType(StatType.STR) * (status.DamageScaling[i].ScalingAmount / 100);
                    break;
                case StatType.DEX:
                    damage += source.GetStatType(StatType.DEX) * (status.DamageScaling[i].ScalingAmount / 100);
                    break;
                case StatType.INT:
                    damage += source.GetStatType(StatType.INT) * (status.DamageScaling[i].ScalingAmount / 100);
                    break;
            }
        }
        return damage;
    }
}
