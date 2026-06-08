using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "Events", menuName = "Events/EventEffect/GainHealthEffect")]
public class GainHealthEffect : EventEffect
{
    public int HealthAmount;
    public override void Execute()
    {
        HealAllPlayersClientRpc();
    }

    [ClientRpc]
    private void HealAllPlayersClientRpc()
    {
        foreach (UnitController controller in BattleManager.instance.FriendlyUnits)
        {
            controller.ServerHeal(HealthAmount, true);
        }
    }
}
