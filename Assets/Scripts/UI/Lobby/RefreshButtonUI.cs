using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RefreshButtonUI : MonoBehaviour
{
    public static RefreshButtonUI Instance { get; private set; }

    public event EventHandler OnRefreshButtonClicked;

    [SerializeField] public Button refreshButton;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        refreshButton.onClick.AddListener(() =>
        {
            OnRefreshButtonClicked?.Invoke(this, EventArgs.Empty);
        });

        // Hide "Refresh Button" button when custom lobby ui has been created
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
