using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public NetworkUI networkUI;

    public static LobbyManager instance;
    public TextMeshProUGUI maximumPlayersText;
    public List<PlayerLobbyState> LobbyPlayerData;
    public LobbyEntryController LobbyEntryControllerPrefab;
    public Transform LobbyEntryParent;

    private Dictionary<ulong, LobbyEntryController> entries = new();
    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public override void OnNetworkSpawn()
    {
        UpdateMaximumPlayersText();
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

    public void ClearEntries()
    {
        entries.Clear();
    }

    public void UpdateMaximumPlayersText()
    {
        maximumPlayersText.text = $"Maximum Players: {networkUI.maxPlayers}";
    }
}
