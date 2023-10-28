using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class LobbyPlayerNameUI : MonoBehaviour
{
    public static LobbyPlayerNameUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI playerNameLabel;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Set player name at lobby to match with the name player typed in at MainMenu
        SetLobbyPlayerName(PlayerPrefs.GetString(PlayerPrefsKey.PLAYER_PREFS_KEY_PLAYER_NAME, ""));
    }

    public void SetLobbyPlayerName(string playerName_in)
    {
        playerNameLabel.text = playerName_in;
    }
}
