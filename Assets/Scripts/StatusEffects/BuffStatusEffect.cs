using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Status Effects", menuName = "StatusEffects/Buff Status Effect")]
public class BuffStatusEffect : StatusEffect
{
    public List<StatModifier> StatModifiers;

    public override void ServerOnApplication(UnitController controller, StatusEffectInstance instance)
    {

    }

    public override void ClientOnApplication(UnitController controller, StatusEffectInstance instance)
    {

    }

    public override void ServerExecuteEffect(UnitController controller, StatusEffectInstance instance)
    {
        foreach (StatModifier mod in StatModifiers)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (mod.stat == StatType.CurrentMana)
                {
                    controller.ServerUpdateMana((int)mod.value);
                }
                else if (mod.stat == StatType.CurrentHealth)
                {
                    controller.ServerHeal(mod.value);
                }
                else
                    controller.StatModifiers.Add(new StatModifier
                    {
                        stat = mod.stat,
                        value = mod.value,
                        sourceId = instance.statusEffectId
                    });
            }
            instance.StatusEffectPower = mod.value;
        }
        controller.CachedStatsDirty = true;
    }

    public override void ClientExecuteEffect(UnitController controller, StatusEffectInstance instance)
    {
        foreach (StatModifier mod in StatModifiers)
        {
            if (mod.stat == StatType.CurrentMana)
            {
                controller.ClientUpdateMana((int)mod.value);
            }

            if (mod.stat == StatType.CurrentHealth)
            {
                HitResult hitResult = new();
                hitResult.DamageType = DamageType.Heal;
                hitResult.DamageSource = DamageSource.StatusEffect;
                hitResult.Damage = -mod.value;
                hitResult.Crit = false;
                hitResult.Dodged = false;
                hitResult.Blocked = false;
                controller.ClientTakeDamage(hitResult);
            }
        }
    }

    public override void ServerRemoveStatus(UnitController controller, StatusEffectInstance instance)
    {
        for (int i = controller.StatModifiers.Count - 1; i >= 0; i--)
        {
            if (controller.StatModifiers[i].sourceId == instance.statusEffectId)
            {
                controller.StatModifiers.RemoveAt(i);
            }
        }
        controller.CachedStatsDirty = true;
    }

    public override string ConstructDescriptionString(StatusEffectInstance statusEffectInstance)
    {
        return StringBuilderUtilities.Build(StatusDescription, statusEffectInstance.StatusEffectPower);
    }
}
