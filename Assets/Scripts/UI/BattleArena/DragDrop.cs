/**********************************************
 * 
 *  DragDrop.cs 
 *  Card's drag and drop behaviour
 * 
 *  製作者：Phansuwan Chaichumphon （ミン）
 * 
 * Todo: Make player not able to drop card in the zone when it's not their turn
 **********************************************/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class DragDrop : NetworkBehaviour
{
    // Prevent mistyping in the future
    private const string DROPZONE_TAG = "DropZone";

    // This gameobject's CardTemplate component
    private CardTemplate cardTemplate;

    // Card order in the hand and dropzone container
    public int cardOrderInHand { get; private set; }
    public int cardOrderInDropZone { get; private set; }

    private bool isDragging = false;
    private bool isDroppable = false;
    private Transform startParent = null;

    private void Awake()
    {
        // Get this gameobject's CardTemplate component
        cardTemplate = GetComponent<CardTemplate>();
    }

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
        cardTemplate.cardNetworkID.Value = gameObject.GetComponent<NetworkObject>().NetworkObjectId;
    }

    // Being called at CardTemplate prefab EventTrigger on Start Drag
    public void StartDrag()
    {
        // Return if not owning the card
        if (!IsOwner) return;

        // Also return if the card is in the dropzone and it is not player's turn
        if (WOEGameManager.Instance.IsInDropZoneContainer(transform) && !WOEGameManager.Instance.IsPlayerTurn()) return;

        // Set start parent/position in case we want to return the card to it's original place
        if (!startParent)
        {
            startParent = transform.parent;
        }

        // Remember our original position
        if (transform.parent == WOEGameManager.Instance.playerHand) 
        {
            cardOrderInHand = transform.GetSiblingIndex();
        }
        else if(transform.parent == WOEGameManager.Instance.dropZoneContainer)
        {
            cardOrderInDropZone = transform.GetSiblingIndex();
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

        // Also return if the card is in the dropzone and it is not player's turn
        if (WOEGameManager.Instance.IsInDropZoneContainer(transform) && !WOEGameManager.Instance.IsPlayerTurn()) return;

        // If the card is over the drop zone
        // AND IF IT IS PLAYER'S TURN
        if (isDroppable && WOEGameManager.Instance.IsPlayerTurn())
        {
            // If we can drop the card in the dropzone, then update the card position in both clients
            WOEGameManager.Instance.Notify_PlaceCardAtServerRpc(cardTemplate.cardNetworkID.Value, WOEGameManager.Instance.dropZoneContainer.gameObject);
        }
        // If the card is not in the drop zone
        else
        {
            // Return/Update card position based on OwnerID
            // and rearrange the card based on original children order
            WOEGameManager.Instance.Notify_ReturnCardToPlayerServerRpc(cardTemplate.cardNetworkID.Value);
        }
    }
}
