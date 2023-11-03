/**********************************************
 * 
 *  PlayerNetwork.cs 
 *  Player's data manager (no visual)
 * 
 *  製作者：Phansuwan Chaichumphon （ミン）
 * 
 **********************************************/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{

    //[SerializeField] private Transform gameManagerTemplate;
    //private Transform gameManager = null;

    // All NetworkVariable must be initialized

    // ******************* Player's variable *******************
    /// <summary> Player's Max HP </summary>
    private NetworkVariable<int> playerMaxHP = new NetworkVariable<int>(40, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary> Player's HP that will be used in battle </summary>
    private NetworkVariable<int> playerHP = new NetworkVariable<int>(99, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary> Player's network ID that will be used to get player reference in WOEGameManager.cs </summary>
    private NetworkVariable<ulong> playerNetworkID = new NetworkVariable<ulong>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        // So this doesn't get destroyed from Lobby scene
        DontDestroyOnLoad(this);
    }

    // Use instead of Start() for online stuff
    public override void OnNetworkSpawn()
    {
        // For debugging/finding player
        // If owner then update player network id and object name
        if (IsOwner)
        {
            // Setting player network id
            playerNetworkID.Value = OwnerClientId;

            // Update player name (owner)
            gameObject.name = "Player" + OwnerClientId;
        }
        // If not owner then check if it is the host or not
        else
        {
            // If it is host and is not the owner of the object, then the object is clearly client's
            if(IsHost)
            {
                gameObject.name = "Player1";
            }
            else
            // If it is client and is not the owner of the object, it's clear host's
            {
                gameObject.name = "Player0";
            }
        }
    }

    private void Update()
    {
        // Cannot process if you are not player's owner
        if(!IsOwner) return;

        /*
        if (Input.GetKeyUp(KeyCode.S))
        {
            // Press S to draw a card
            SpawnGameManagerServerRpc();
        }
        */

        if (Input.GetKeyUp(KeyCode.D))
        {
            // Press D to draw a card
            WOEGameManager.Instance.Notify_DrawServerRpc(new ServerRpcParams());
        }
    }

    public int GetPlayerMaxHP()
    {
        return playerMaxHP.Value;
    }

    public int GetPlayerHP()
    {
        return playerHP.Value;
    }

    public ulong GetPlayerNetworkID()
    {
        return playerNetworkID.Value;
    }

    /*
    [ServerRpc]
    private void SpawnGameManagerServerRpc()
    {
        gameManager = Instantiate<Transform>(gameManagerTemplate, Vector3.zero, Quaternion.identity);
        gameManager.GetComponent<NetworkObject>().Spawn(true);
    }
    */
}
