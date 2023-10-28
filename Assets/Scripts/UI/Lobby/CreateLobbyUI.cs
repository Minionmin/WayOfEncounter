using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CreateLobbyUI : MonoBehaviour
{
    public static CreateLobbyUI Instance { get; private set; }

    public event EventHandler OnCreateLobbyClicked;

    [SerializeField] private Button createLobbyButton;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        createLobbyButton.onClick.AddListener(() =>
        {
            OnCreateLobbyClicked?.Invoke(this, EventArgs.Empty);
        });

        // Hide "Create Lobby" button when custom lobby ui has been created
        LobbyManager.Instance.OnJoinedLobby += LobbyManager_OnJoinedLobby;
    }

    private void LobbyManager_OnJoinedLobby(object sender, EventArgs e)
    {
        Hide();
    }

    // Hide/Show Create Lobby Button
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}
