using Unity.Netcode;
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

    public abstract void ServerExecuteEffect(UnitController controller);

    public abstract void ClientExecuteEffect(UnitController controller);
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
    OnHit
}
