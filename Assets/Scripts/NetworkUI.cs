using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkUI : NetworkBehaviour
{
    public int RequiredNumberOfPlayers;

    void Awake()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
    }

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

        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnManager.instance.HandleClientConnected(id);
        }
        yield return null;
    }

    public void StartGame()
    {
        ProgressionManager.instance.LoadFirstRoom();
    }
}
