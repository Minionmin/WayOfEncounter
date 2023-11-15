/**********************************************
 * 
 *  ActionUI.cs 
 *  In game action buttons UI
 * 
 *  ����ҁFPhansuwan Chaichumphon �i�~���j
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
    }
}
