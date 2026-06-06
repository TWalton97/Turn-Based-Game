using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager instance;
    public NetworkUI NetworkUI;

    public GameObject MainMenuUI;
    public GameObject StartGameUI;
    public GameObject JoinGameUI;
    public GameObject LobbyMenuUI;

    //Start Game UI
    public TextMeshProUGUI PlayerCountText;
    private int currentPlayerCount = 4;

    void Awake()
    {
        if (instance != null)
            instance = this;
    }

    public void OpenMainMenu()
    {
        MainMenuUI.SetActive(true);
        StartGameUI.SetActive(false);
        JoinGameUI.SetActive(false);
        LobbyMenuUI.SetActive(false);
    }

    public void OpenStartGameMenu()
    {
        MainMenuUI.SetActive(false);
        StartGameUI.SetActive(true);
        JoinGameUI.SetActive(false);
        LobbyMenuUI.SetActive(false);
    }

    public void OpenJoinGameMenu()
    {
        MainMenuUI.SetActive(false);
        StartGameUI.SetActive(false);
        JoinGameUI.SetActive(true);
        LobbyMenuUI.SetActive(false);
    }

    public void OpenLobbyMenu()
    {
        MainMenuUI.SetActive(false);
        StartGameUI.SetActive(false);
        JoinGameUI.SetActive(false);
        LobbyMenuUI.SetActive(true);
    }

    public void IncreasePlayerCount()
    {
        currentPlayerCount = Mathf.Clamp(currentPlayerCount + 1, 1, 4);
        PlayerCountText.text = currentPlayerCount.ToString();
        NetworkUI.SetNumberOfPlayers(currentPlayerCount);
    }

    public void DecreasePlayerCount()
    {
        currentPlayerCount = Mathf.Clamp(currentPlayerCount - 1, 1, 4);
        PlayerCountText.text = currentPlayerCount.ToString();
        NetworkUI.SetNumberOfPlayers(currentPlayerCount);
    }
}
