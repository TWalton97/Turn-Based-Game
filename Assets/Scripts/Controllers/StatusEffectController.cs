using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;
using System.Collections;
using UnityEngine;

public class StatusEffectController : NetworkBehaviour
{
    private UnitController unitController;
    public List<StatusEffectInstance> ServerActiveStatusEffects = new();
    public List<StatusEffectInstance> ClientActiveStatusEffectViews = new();

    public Action OnStatusEffectsChanged;

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    public void ServerApplyStatusEffect(StatusEffect statusEffect, ulong sourceUnitId, FixedString64Bytes statusInstanceId, float statusEffectPower = 1)
    {
        // If there is already an existing status effect of this type, we just update that one
        // StatusEffectInstance existingStatusEffectInstance = ActiveStatusEffects.Find(t => t.StatusEffect == statusEffect);
        // if (existingStatusEffectInstance != null)
        // {
        //     existingStatusEffectInstance.RemainingNumberOfTurns = statusEffect.NumberOfTurns;
        //     existingStatusEffectInstance.StatusEffectPower = Mathf.Max(existingStatusEffectInstance.StatusEffectPower, statusEffectPower);
        //     OnStatusEffectsChanged?.Invoke();
        //     return;
        // }

        //If there is no existing status effect of this type, we create a new one
        UnitController sourceUnitController = NetworkUtilities.GetUnitControllerById(sourceUnitId);
        StatusEffectInstance newStatusEffectInstance = new StatusEffectInstance(unitController, sourceUnitController, statusEffect, statusEffect.NumberOfTurns, statusEffectPower, statusInstanceId);
        ServerActiveStatusEffects.Add(newStatusEffectInstance);

        if (statusEffect.ActivationTime == ActivationTime.OnApplication)
        {
            newStatusEffectInstance.ServerExecuteEffect(unitController);
        }
    }

    public void ClientApplyStatusEffect(StatusEffect statusEffect, ulong sourceUnitId, FixedString64Bytes statusInstanceId, float statusEffectPower = 1)
    {
        UnitController sourceUnitController = NetworkUtilities.GetUnitControllerById(sourceUnitId);
        StatusEffectInstance newStatusEffectInstance = new StatusEffectInstance(unitController, sourceUnitController, statusEffect, statusEffect.NumberOfTurns, statusEffectPower, statusInstanceId);
        ClientActiveStatusEffectViews.Add(newStatusEffectInstance);

        if (statusEffect.ActivationTime == ActivationTime.OnApplication)
        {
            newStatusEffectInstance.ClientExecuteEffect(unitController);
        }

        OnStatusEffectsChanged?.Invoke();
    }

    public void ServerOnTurnStarted()
    {
        for (int i = ServerActiveStatusEffects.Count - 1; i >= 0; i--)
        {
            var s = ServerActiveStatusEffects[i];

            if (s.StatusEffect.ActivationTime == ActivationTime.StartOfTurn)
                s.ServerExecuteEffect(s.UnitController);

            s.RemainingNumberOfTurns--;

            if (s.RemainingNumberOfTurns <= 0)
            {
                if (s.StatusEffect.ActivationTime == ActivationTime.OnExpire)
                    s.ServerExecuteEffect(s.UnitController);

                OnStatusInstanceExpiredClientRpc(ServerActiveStatusEffects[i].statusEffectId);

                s.StatusEffect.ServerRemoveStatus(s.UnitController, s);
                ServerActiveStatusEffects.RemoveAt(i);
            }
        }
    }

    [ClientRpc]
    private void OnStatusInstanceExpiredClientRpc(FixedString64Bytes statusInstanceId)
    {
        for (int i = 0; i < ClientActiveStatusEffectViews.Count; i++)
        {
            if (ClientActiveStatusEffectViews[i].statusEffectId == statusInstanceId)
            {
                ClientActiveStatusEffectViews[i].isServerExpired = true;
            }
        }
    }

    public IEnumerator ClientOnTurnStarted()
    {
        for (int i = ClientActiveStatusEffectViews.Count - 1; i >= 0; i--)
        {
            var status = ClientActiveStatusEffectViews[i];

            if (status.StatusEffect.ActivationTime == ActivationTime.StartOfTurn)
                status.ClientExecuteEffect(status.UnitController);

            status.RemainingNumberOfTurns--;

            if (status.RemainingNumberOfTurns <= 0 || status.isServerExpired)
            {
                if (status.StatusEffect.ActivationTime == ActivationTime.OnExpire)
                {
                    status.ClientExecuteEffect(status.UnitController);
                    yield return new WaitForSeconds(0.7f);
                }
                ClientActiveStatusEffectViews.RemoveAt(i);
                yield return null;
            }
            yield return null;
        }
        OnStatusEffectsChanged?.Invoke();
        yield return null;
    }

    public void ServerOnTurnEnded()
    {
        for (int i = ServerActiveStatusEffects.Count - 1; i >= 0; i--)
        {
            var s = ServerActiveStatusEffects[i];

            if (s.StatusEffect.ActivationTime == ActivationTime.EndOfTurn)
                s.ServerExecuteEffect(s.UnitController);
        }
    }

    public void ClientOnTurnEnded()
    {
        for (int i = ClientActiveStatusEffectViews.Count - 1; i >= 0; i--)
        {
            var s = ClientActiveStatusEffectViews[i];

            if (s.StatusEffect.ActivationTime == ActivationTime.EndOfTurn)
            {
                s.ClientExecuteEffect(s.UnitController);
                OnStatusEffectsChanged?.Invoke();
            }
        }
    }

    public void ServerOnHit()
    {
        for (int i = ServerActiveStatusEffects.Count - 1; i >= 0; i--)
        {
            var s = ServerActiveStatusEffects[i];

            if (s.StatusEffect.ActivationTime == ActivationTime.OnTakeHit)
                s.ServerExecuteEffect(s.UnitController);
        }
    }

    public void ClientOnHit()
    {
        for (int i = ClientActiveStatusEffectViews.Count - 1; i >= 0; i--)
        {
            var s = ClientActiveStatusEffectViews[i];

            if (s.StatusEffect.ActivationTime == ActivationTime.OnTakeHit)
                s.ClientExecuteEffect(s.UnitController);

            OnStatusEffectsChanged?.Invoke();
        }
    }

    public void ServerOnAttack()
    {
        for (int i = ServerActiveStatusEffects.Count - 1; i >= 0; i--)
        {
            var s = ServerActiveStatusEffects[i];

            if (s.StatusEffect.ActivationTime == ActivationTime.OnAttack)
                s.ServerExecuteEffect(s.UnitController);
        }
    }

    public void ClientOnAttack()
    {
        for (int i = ClientActiveStatusEffectViews.Count - 1; i >= 0; i--)
        {
            var s = ClientActiveStatusEffectViews[i];

            if (s.StatusEffect.ActivationTime == ActivationTime.OnAttack)
                s.ClientExecuteEffect(s.UnitController);
        }
    }

    public void ClearAllStatusEffects()
    {
        foreach (StatusEffectInstance instance in ServerActiveStatusEffects)
        {
            instance.StatusEffect.ServerRemoveStatus(unitController, instance);
        }
        ServerActiveStatusEffects.Clear();
        ClientActiveStatusEffectViews.Clear();
    }
}
