using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyObjectUI : MonoBehaviour
{
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private TextMeshProUGUI lobbyTitleLabel;
    [SerializeField] private TextMeshProUGUI lobbyMaxPlayerLabel;

    private Lobby lobby;

    void Start()
    {
        joinLobbyButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.JoinLobbyById(lobby.Id);
        });
    }

    public void UpdateLobby(Lobby lobby)
    {
        this.lobby = lobby;

        lobbyTitleLabel.text = lobby.Name;
        lobbyMaxPlayerLabel.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
    }

    public Lobby GetLobby()
    {
        return lobby;
    }
}
