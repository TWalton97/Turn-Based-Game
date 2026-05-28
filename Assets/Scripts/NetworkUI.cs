using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkUI : NetworkBehaviour
{
    public int RequiredNumberOfPlayers;

    public void StartHost()
    {
        SpawnManager.instance.BindNetworkEvents();

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

    public void StartGame()
    {
        ProgressionManager.instance.LoadFirstRoom();
    }
}
