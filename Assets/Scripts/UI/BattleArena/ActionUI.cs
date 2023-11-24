/**********************************************
 * 
 *  ActionUI.cs 
 *  In game action buttons UI
 * 
 *  製作者：Phansuwan Chaichumphon （ミン）
 * 
 **********************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ActionUI : NetworkBehaviour
{
    private const string COMFIRM_BUTTON = "ConfirmButton";

    public event EventHandler OnConfirmButtonClicked;

    public static ActionUI Instance { get; private set; }

    [SerializeField] private Button confirmButton;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        confirmButton.onClick.AddListener(() =>
        {
            OnConfirmButtonClicked?.Invoke(this, EventArgs.Empty);
        });

        // Initialize confirm button as uninteractable
        confirmButton.interactable = false;
    }

    public override void OnNetworkSpawn()
    {

    }

    private void WOEGameManager_OnGameStateChange(object sender, EventArgs e)
    {
        // Make the confirm button interactable if it's player's turn, the opposite if it's not
        if (!WOEGameManager.Instance.IsPlayerTurn())
        {
            confirmButton.interactable = false;
        }
        else
        {
            confirmButton.interactable = true;
        }
    }

    public void Initialize()
    {
        // Confirm button is interactable or not will be depending on game state
        WOEGameManager.Instance.OnGameStateChange += WOEGameManager_OnGameStateChange;
    }
}
