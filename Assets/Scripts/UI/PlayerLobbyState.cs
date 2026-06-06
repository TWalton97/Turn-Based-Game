using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerLobbyState : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> playerName = new();
    public NetworkVariable<PlayerClass> playerClass = new();
    public NetworkVariable<bool> readyState = new();

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            LobbyManager.instance.CreateOrUpdateLobbyEntry(this);
        }
    }

    [ServerRpc]
    public void SetNameServerRpc(FixedString64Bytes name)
    {
        playerName.Value = name;
    }

    [ServerRpc]
    public void ChangeClassServerRpc(int newValue)
    {
        playerClass.Value = (PlayerClass)newValue;
    }


    [ServerRpc]
    public void SetReadyServerRpc(bool value)
    {
        readyState.Value = value;
    }
}
