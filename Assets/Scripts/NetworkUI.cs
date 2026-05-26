using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
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

        ProgressionManager.instance.LoadFirstRoom();
        yield return null;
    }
}
