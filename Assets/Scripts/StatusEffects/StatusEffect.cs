using UnityEngine;

[System.Serializable]
public abstract class StatusEffect : ScriptableObject
{
    public string StatusEffectName;
    [TextArea] public string StatusDescription;
    public Sprite StatusSprite;

    public BuffType BuffType;
    public ActivationTime ActivationTime;
    public int NumberOfTurns;

    public abstract void ServerOnApplication(UnitController controller, StatusEffectInstance instance);
    public abstract void ClientOnApplication(UnitController controller, StatusEffectInstance instance);

    public abstract void ServerExecuteEffect(UnitController controller, StatusEffectInstance instance);
    public abstract void ClientExecuteEffect(UnitController controller, StatusEffectInstance instance);

    public abstract void ServerRemoveStatus(UnitController controller, StatusEffectInstance instance);

    public abstract string ConstructDescriptionString(StatusEffectInstance statusEffectInstance);

    protected virtual void OnEnable()
    {
        StatusDatabase.RegisterStatusEffect(this);
    }
}

public enum BuffType
{
    Buff,
    Debuff
}

public enum ActivationTime
{
    StartOfTurn,
    EndOfTurn,
    OnHit,
    OnApplication,
    OnExpire
}
