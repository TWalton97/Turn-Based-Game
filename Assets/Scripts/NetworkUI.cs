using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using TMPro;
using UnityEngine.SceneManagement;

public class NetworkUI : NetworkBehaviour
{
    public MainMenuManager mainMenuManager;
    public LobbyManager lobbyManager;

    //Host menu
    public int maxPlayers = 4;
    public TextMeshProUGUI joinCodeText;

    //Join menu
    public TMP_InputField inputField;

    private bool gameStarted = false;

    void Awake()
    {
        DontDestroyOnLoad(this);
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    }

    private async void Start()
    {
        await InitUGS();
    }

    public void SetNumberOfPlayers(int numberOfPlayers)
    {
        maxPlayers = numberOfPlayers;
    }

    private async Task InitUGS()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("UGS Ready");
    }

    public void CheckSelectedPlayerCount()
    {
        if (maxPlayers == 1)
        {
            NetworkManager.Singleton.StartHost();
            StartGame();
        }
        else
        {
            StartHostWithRelay();
            mainMenuManager.OpenLobbyMenu();
        }
    }

    // HOST
    public async void StartHostWithRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        joinCodeText.text = $"Join Code: {joinCode}";

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );

        NetworkManager.Singleton.StartHost();
    }

    // CLIENT
    public async void JoinRelayWithCode()
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(inputField.text);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            mainMenuManager.OpenLobbyMenu();
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to join relay: {e.Message}");
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (gameStarted)
        {
            response.Approved = false;
            response.Reason = "Game already in progress";
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = true;
    }

    public void StartGame()
    {
        if (!IsHost)
            return;

        if (!CanStartGame())
            return;

        gameStarted = true;
        NetworkManager.SceneManager.LoadScene("S_BattlePrototype", LoadSceneMode.Single);
    }

    bool CanStartGame()
    {
        var players = FindObjectsOfType<PlayerLobbyState>();

        foreach (var p in players)
        {
            if (!p.readyState.Value)
                return false;
        }

        return players.Length > 0;
    }
}
