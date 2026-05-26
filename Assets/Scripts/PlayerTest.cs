using Unity.Netcode;
using UnityEngine;

public class PlayerTest : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        Debug.Log($"Spawned player. Owner: {OwnerClientId}, Local: {IsOwner}");
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestServerRpc();
        }
    }

    [ServerRpc]
    void TestServerRpc()
    {
        Debug.Log($"Server received input from {OwnerClientId}");
    }
}
