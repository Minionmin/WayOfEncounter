using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JoinedLobbyUI : MonoBehaviour
{
    public static JoinedLobbyUI Instance { get; private set; }

    public event EventHandler OnStartButtonClicked;

    [SerializeField] private Transform playerDetailTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI lobbyNameLabel;
    [SerializeField] private Button startButton;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Update lobby when host has joined the lobby
        LobbyManager.Instance.OnJoinedLobby += LobbyManager_OnJoinedLobby;

        // Update lobby when the lobby is polled (every 1.1f seconds at LobbyManager.cs)
        LobbyManager.Instance.OnJoinedLobbyUpdate += LobbyManager_OnJoinedLobbyUpdate;

        // Load "BattleArena" Scene after started the game
        LobbyManager.Instance.OnGameStart += LobbyManager_OnGameStart;

        startButton.onClick.AddListener(() =>
        {
            OnStartButtonClicked?.Invoke(this, EventArgs.Empty);
        });

        Hide();
    }

    private void UpdateLobby(Lobby lobby_in)
    {
        // Clear player list objects first before creating all again (Update)
        ClearLobby();

        // Update lobby name's UI
        lobbyNameLabel.text = lobby_in.Name;

        // Update player UI after updated lobby UI
        foreach (Player player in lobby_in.Players)
        {
            // Instantiate player list object
            Transform playerSingleTransform = Instantiate(playerDetailTemplate, container);

            // Make player list object visible
            playerSingleTransform.gameObject.SetActive(true);

            // Update player per player in lobby
            PlayerListObject playerListObjectUI = playerSingleTransform.GetComponent<PlayerListObject>();
            playerListObjectUI.UpdatePlayer(player);
        }
    }

    // Clear player list objects first before creating all again (Update)
    private void ClearLobby()
    {
        foreach (Transform child in container)
        {
            if (child == playerDetailTemplate) continue;
            Destroy(child.gameObject);
        }
    }

    private void LobbyManager_OnJoinedLobbyUpdate(object sender, LobbyManager.LobbyEventArgs e)
    {
        UpdateLobby(e.lobby);
    }

    private void LobbyManager_OnJoinedLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        // Show joined lobby UI
        Show();

        // Update lobby detail
        UpdateLobby(e.lobby);
    }

    private void LobbyManager_OnGameStart(object sender, EventArgs e)
    {
        SceneManager.LoadScene("BattleArena");
    }

    // Hide/Show LobbyListUI
    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }
}
