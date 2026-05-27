using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkUI : NetworkBehaviour
{
    public int RequiredNumberOfPlayers;

    public void StartHost()
    {
        SpawnManager.instance.BindNetworkEvents();
        NetworkManager.Singleton.OnClientConnectedCallback += CheckNumberOfPlayers;

        NetworkManager.Singleton.StartHost();

        StartCoroutine(WaitForServer());
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    private IEnumerator WaitForServer()
    {
        while (!NetworkManager.Singleton.IsServer)
            yield return null;

        Debug.Log("Server started");
        yield return null;
    }

    private void CheckNumberOfPlayers(ulong clientId)
    {
        if (!IsServer)
            return;

        Debug.Log($"Current number of players is {NetworkManager.Singleton.ConnectedClientsList.Count}");

        if (NetworkManager.Singleton.ConnectedClientsList.Count >= RequiredNumberOfPlayers)
        {
            Debug.Log($"Enough players connecting - loading first room");
            ProgressionManager.instance.LoadFirstRoom();
        }
    }
}
