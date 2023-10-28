using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomLobbyUI : MonoBehaviour
{
    public static CustomLobbyUI Instance { get; private set; }

    // Events
    public event EventHandler OnCustomLobbyShow;
    public event EventHandler<OnCreateButtonClickedEventArgs> OnCreateButtonClicked;
    public class OnCreateButtonClickedEventArgs : EventArgs
    {
        public string lobbyName;
    }

    // Custom lobby content variables
    [SerializeField] private TMP_InputField lobbyNameInputField;
    [SerializeField] private Button createButton;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // LobbyManager.cs will create a lobby with the name player typed in
        createButton.onClick.AddListener(() =>
        {
            OnCreateButtonClicked?.Invoke(this, new OnCreateButtonClickedEventArgs { lobbyName = lobbyNameInputField.text });
        });

        // Create Custom lobby ui when create lobby button is clicked
        CreateLobbyUI.Instance.OnCreateLobbyClicked += CreateLobbyButton_OnCreateLobbyClicked;

        // Hide Custom Lobby UI when the lobby is created and joined
        LobbyManager.Instance.OnJoinedLobby += LobbyManager_OnJoinedLobby;

        Hide();
    }

    private void CreateLobbyButton_OnCreateLobbyClicked(object sender, EventArgs e)
    {
        Show();
    }

    private void LobbyManager_OnJoinedLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        Hide();
    }

    // Hide/Show Custom Lobby UI
    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
        // No one is using this event
        OnCustomLobbyShow?.Invoke(this, EventArgs.Empty);
    }
}
