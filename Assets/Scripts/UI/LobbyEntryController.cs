using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LobbyEntryController : MonoBehaviour
{
    //Name selection
    public TMP_InputField NameInputField;

    //Class selection
    public TextMeshProUGUI ClassNameText;
    public Button MoveClassSelectionLeftButton;
    public Button MoveClassSelectionRightButton;

    //Ready toggle
    public Toggle ReadyToggle;

    private PlayerLobbyState playerLobbyState;

    public void Bind(PlayerLobbyState playerLobbyState)
    {
        Unbind();

        bool isOwner = playerLobbyState.IsOwner;

        NameInputField.interactable = isOwner;
        MoveClassSelectionLeftButton.interactable = isOwner;
        MoveClassSelectionRightButton.interactable = isOwner;
        ReadyToggle.interactable = isOwner;

        this.playerLobbyState = playerLobbyState;

        playerLobbyState.playerName.OnValueChanged += OnNameChanged;
        playerLobbyState.playerClass.OnValueChanged += OnClassChanged;
        playerLobbyState.readyState.OnValueChanged += OnReadyChanged;
        playerLobbyState.OnAnyDespawned += HandlePlayerDespawned;

        NameInputField.text = playerLobbyState.playerName.Value.ToString();
        ClassNameText.text = playerLobbyState.playerClass.Value.ToString();
        ReadyToggle.isOn = playerLobbyState.readyState.Value;

        NameInputField.onEndEdit.AddListener(OnNameEdited);
        MoveClassSelectionLeftButton.onClick.AddListener(OnPrevClass);
        MoveClassSelectionRightButton.onClick.AddListener(OnNextClass);
        ReadyToggle.onValueChanged.AddListener(OnReadyToggled);

        RefreshAll();
    }

    void RefreshAll()
    {
        OnNameChanged(default, playerLobbyState.playerName.Value);
        OnClassChanged(default, playerLobbyState.playerClass.Value);
        OnReadyChanged(default, playerLobbyState.readyState.Value);
    }

    private void OnReadyChanged(bool previousValue, bool newValue)
    {
        ReadyToggle.isOn = newValue;
    }

    private void OnClassChanged(PlayerClass previousValue, PlayerClass newValue)
    {
        ClassNameText.text = newValue.ToString();
    }

    private void OnNameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        NameInputField.text = newValue.ToString();
    }

    private void Unbind()
    {
        if (playerLobbyState == null) return;

        playerLobbyState.playerName.OnValueChanged -= OnNameChanged;
        playerLobbyState.playerClass.OnValueChanged -= OnClassChanged;
        playerLobbyState.readyState.OnValueChanged -= OnReadyChanged;
        playerLobbyState.OnAnyDespawned -= HandlePlayerDespawned;

        NameInputField.onEndEdit.RemoveAllListeners();
        MoveClassSelectionLeftButton.onClick.RemoveAllListeners();
        MoveClassSelectionRightButton.onClick.RemoveAllListeners();
        ReadyToggle.onValueChanged.RemoveAllListeners();

        playerLobbyState = null;
    }

    public void HandlePlayerDespawned(PlayerLobbyState state)
    {
        if (state == playerLobbyState && this != null)
        {
            Unbind();
            Destroy(gameObject);
        }
    }

    public void OnNameEdited(string newName)
    {
        if (!playerLobbyState.IsOwner) return;

        playerLobbyState.SetNameServerRpc(newName);
    }

    public void OnNextClass()
    {
        if (!playerLobbyState.IsOwner) return;

        PlayerClass next = GetNextClass(playerLobbyState.playerClass.Value);
        playerLobbyState.ChangeClassServerRpc(GetClassIndex(next));
    }

    public void OnPrevClass()
    {
        if (!playerLobbyState.IsOwner) return;

        PlayerClass prev = GetPreviousClass(playerLobbyState.playerClass.Value);
        playerLobbyState.ChangeClassServerRpc(GetClassIndex(prev));
    }

    public void OnReadyToggled(bool value)
    {
        if (!playerLobbyState.IsOwner) return;

        playerLobbyState.SetReadyServerRpc(value);
    }

    int GetClassIndex(PlayerClass cls)
    {
        return (int)cls;
    }

    PlayerClass GetClassFromIndex(int index)
    {
        return (PlayerClass)index;
    }

    int GetClassCount()
    {
        return Enum.GetValues(typeof(PlayerClass)).Length;
    }

    PlayerClass GetNextClass(PlayerClass current)
    {
        int count = GetClassCount();
        int next = ((int)current + 1) % count;
        return (PlayerClass)next;
    }

    PlayerClass GetPreviousClass(PlayerClass current)
    {
        int count = GetClassCount();
        int prev = ((int)current - 1 + count) % count;
        return (PlayerClass)prev;
    }
}
