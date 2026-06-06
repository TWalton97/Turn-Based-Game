using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager instance;
    public TextMeshProUGUI connectedPlayersText;
    public NetworkVariable<int> ConnectedPlayerCount;
    public List<PlayerLobbyState> LobbyPlayerData;
    public LobbyEntryController LobbyEntryControllerPrefab;
    public Transform LobbyEntryParent;

    private Dictionary<ulong, LobbyEntryController> entries = new();
    void Awake()
    {
        if (instance == null)
            instance = this;

        ConnectedPlayerCount.OnValueChanged += UpdateConnectedPlayersText;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public void BuildAllEntries()
    {
        var players = FindObjectsOfType<PlayerLobbyState>();

        foreach (var player in players)
        {
            CreateOrUpdateLobbyEntry(player);
        }
    }

    public void CreateOrUpdateLobbyEntry(PlayerLobbyState player)
    {
        ulong clientId = player.OwnerClientId;

        if (entries.TryGetValue(clientId, out LobbyEntryController existingUI))
        {
            existingUI.Bind(player);
            return;
        }

        LobbyEntryController newEntry = Instantiate(LobbyEntryControllerPrefab, LobbyEntryParent);

        newEntry.Bind(player);

        entries.Add(clientId, newEntry);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            ConnectedPlayerCount.Value = NetworkManager.Singleton.ConnectedClients.Count;
        }
    }

    public void UpdateConnectedPlayersText(int oldValue, int newValue)
    {
        connectedPlayersText.text = $"Connected players: {newValue}";
    }
}

public enum PlayerClass
{
    Warrior,
    Rogue,
    Priest
}
