using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackButtonUI : MonoBehaviour
{
    public static BackButtonUI Instance { get; private set; }

    public event EventHandler OnBackButtonClicked;

    [SerializeField] public Button backButton;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        backButton.onClick.AddListener(() =>
        { 
            OnBackButtonClicked?.Invoke(this, EventArgs.Empty);
        });

        // Hide "Back Button" button when custom lobby ui has been created
        LobbyManager.Instance.OnJoinedLobby += LobbyManager_OnJoinedLobby;
    }

    private void LobbyManager_OnJoinedLobby(object sender, EventArgs e)
    {
        Hide();
    }

    // Hide/Show Create Lobby Button
    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }
}
