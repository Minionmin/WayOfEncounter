using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UserAuthenticationUI : MonoBehaviour
{

    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private Button startGameButton;

    private void Start()
    {
        startGameButton.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString(PlayerPrefsKey.PLAYER_PREFS_KEY_PLAYER_NAME, playerNameInputField.text);
            SceneManager.LoadScene("Lobby");
        });
    }
}
