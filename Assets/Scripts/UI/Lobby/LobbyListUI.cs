using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyListUI : MonoBehaviour
{
    public static LobbyListUI Instance { get; private set; }

    [SerializeField] private Transform LobbyObjectTemplate;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LobbyManager.Instance.OnLobbyListChanged += LobbyManager_OnLobbyListChanged;
        LobbyManager.Instance.OnJoinedLobby += LobbyManager_OnJoinedLobby;
    }

    // Hide Player's Lobby List when joining a lobby
    private void LobbyManager_OnJoinedLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        Hide();
    }

    // Will trigger on LobbyManager class RefreshLobbies function
    private void LobbyManager_OnLobbyListChanged(object sender, LobbyManager.OnLobbyListChangedEventArgs e)
    {
        const float secondToWait = 1f;
        // Get List<Lobby> returned from EventArgs and pass them to update the UI
        StartCoroutine(UpdateLobbyListUI(secondToWait, e.lobbyList));
    }

    public IEnumerator UpdateLobbyListUI(float secondToWait, List<Lobby> lobbyList_in)
    {
        // wait for n second before updating UI 
        yield return new WaitForSeconds(secondToWait);

        // Destroy old lobbies before updating new one
        foreach(Transform child in transform)
        {
            if (child == LobbyObjectTemplate) continue;

            Destroy(child.gameObject);
        }

        // for each new lobby we got from query, create and update it's title, maxplayer and set to visible
        foreach(Lobby lobby in lobbyList_in)
        {
            GameObject lobbyObject = Instantiate(LobbyObjectTemplate.gameObject, transform);
            lobbyObject.SetActive(true);
            LobbyObjectUI lobbyObjectUI = lobbyObject.GetComponent<LobbyObjectUI>();
            lobbyObjectUI.UpdateLobby(lobby);
        }
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
