/**********************************************
 * 
 *  DragDrop.cs 
 *  Card's drag and drop behaviour
 * 
 *  製作者：Phansuwan Chaichumphon （ミン）
 * 
 **********************************************/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DragDrop : NetworkBehaviour
{
    /// <summary> Player's network ID that will be used to get player reference in WOEGameManager.cs </summary>
    public NetworkVariable<ulong> cardNetworkID = new NetworkVariable<ulong>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Prevent mistyping in the future
    private const string DROPZONE_TAG = "DropZone";

    private bool isDragging = false;
    private bool isDroppable = false;
    private Transform startParent = null;

    private void Update()
    {
        // Return if not owning the card
        if (!IsOwner) return;

        if (isDragging)
        {
            Drag();
        }
    }

    // Make the card droppable upon entering drop zone
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == DROPZONE_TAG)
        {
            isDroppable = true;
        }
    }

    // Make the card still droppable after exit certain part of trigger box (ex.in the middle of drop zone)
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isDroppable && collision.tag == DROPZONE_TAG)
        {
            isDroppable = true;
        }
    }

    // Make the card un-droppable upon exiting drop zone
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.tag == DROPZONE_TAG)
        {
            isDroppable = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        cardNetworkID.Value = gameObject.GetComponent<NetworkObject>().NetworkObjectId;
    }

    // Being called at CardTemplate prefab EventTrigger on Start Drag
    public void StartDrag()
    {
        // Return if not owning the card
        if (!IsOwner) return;

        // Set start parent/position in case we want to return the card to it's original place
        if (!startParent)
        {
            startParent = transform.parent;
        }
        
        // Start Drag() function
        isDragging = true;
    }

    // Drag card (being called in Update)
    private void Drag()
    {
        // Return if not owning the card
        if (!IsOwner) return;

        // Set the card position according to player's mouse position
        transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        // Make the card visible above all other UIs
        transform.SetParent(WOEGameManager.Instance.mainCanvas, true);
    }

    // Being called at CardTemplate prefab EventTrigger on End Drag
    public void EndDrag()
    {
        // Return if not owning the card
        if (!IsOwner) return;

        // Reset isDragging
        isDragging = false;
        
        // If the card is over the drop zone
        if(isDroppable)
        {
            // Set the card's parent to drop zone
            transform.SetParent(WOEGameManager.Instance.dropZoneContainer, false);
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;

            WOEGameManager.Instance.Notify_PlaceCardAtServerRpc(cardNetworkID.Value, WOEGameManager.Instance.dropZoneContainer.gameObject);
        }
        // If the card is not in the drop zone
        else
        {
            // Return the card back to the original parent
            transform.SetParent(startParent, false);
        }
    }
}
