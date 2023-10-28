/**********************************************
 * 
 *  NetworkManagerUI.cs 
 *  For testing NetworkManager (UI)
 * 
 *  製作者：Phansuwan Chaichumphon （ミン）
 *  制作日：2023/10/26
 * 
 **********************************************/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : NetworkBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Transform gameManagerTemplate;
    private Transform gameManager = null;

    private void Awake()
    {
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });

        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });
    }

    public override void OnNetworkSpawn()
    {
        if (!IsHost) return;
        if (gameManager != null) return;

        gameManager = Instantiate<Transform>(gameManagerTemplate, Vector3.zero, Quaternion.identity);
        gameManager.GetComponent<NetworkObject>().Spawn(true);
    }
}
