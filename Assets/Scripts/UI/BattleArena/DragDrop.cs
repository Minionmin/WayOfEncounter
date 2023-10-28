/**********************************************
 * 
 *  DragDrop.cs 
 *  Card's drag and drop behaviour
 * 
 *  ����ҁFPhansuwan Chaichumphon �i�~���j
 * 
 **********************************************/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DragDrop : NetworkBehaviour
{
    // Prevent mistyping in the future
    private const string DROPZONE_TAG = "DropZone";

    private bool isDragging = false;
    private bool isDroppable = false;
    private Vector2 startPosition;
    private Transform startParent;

    private void Update()
    {
        if(isDragging)
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

    // Being called at CardTemplate prefab EventTrigger on Start Drag
    public void StartDrag()
    {
        // Set start parent/position in case we want to return the card to it's original place
        // (When the card is not droppable)
        if(transform.parent != WOEGameManager.Instance.dropZone)
        {
            startParent = transform.parent;
            startPosition = transform.localPosition;
        }
        
        // Start Drag() function
        isDragging = true;
    }

    // Drag card (being called in Update)
    private void Drag()
    {
        // Set the card position according to player's mouse position
        transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        // Make the card visible above all other UIs
        transform.SetParent(WOEGameManager.Instance.mainCanvas, true);
    }

    // Being called at CardTemplate prefab EventTrigger on End Drag
    public void EndDrag()
    {
        isDragging = false;
        
        // If the card is over the drop zone
        if(isDroppable)
        {
            // Set the card's parent to drop zone
            transform.SetParent(WOEGameManager.Instance.dropZoneContainer, false);
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
        }
        // If the card is not in the drop zone
        else
        {
            // Return the card back to the original parent
            transform.SetParent(startParent, false);
            transform.position = startPosition;
        }
    }
}
