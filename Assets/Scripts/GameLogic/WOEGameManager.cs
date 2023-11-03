/**********************************************
 * 
 *  WOEGameManager.cs 
 *  Game state manager
 * 
 *  製作者：Phansuwan Chaichumphon （ミン）
 * 
 **********************************************/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class WOEGameManager : NetworkBehaviour
{
    // UI name (String)
    private const string MAIN_CANVAS = "MainCanvas";
    private const string BACKGROUND = "Background";
    private const string PLAYER_CHARACTER = "PlayerCharacter";
    private const string ENEMY_CHARACTER = "EnemyCharacter";
    private const string DROP_ZONE = "DropZone";
    private const string DROP_ZONE_CONTAINER = "DropZoneContainer";
    private const string PLAYER_ZONE = "PlayerZone";
    private const string ENEMY_ZONE = "EnemyZone";
    private const string PLAYER_HAND = "PlayerHand";
    private const string ENEMY_HAND = "EnemyHand";

    [Header("Deck/Card template")]
    [SerializeField] private Transform cardTemplateTransform;
    NetworkVariable<int> randomDeckIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Deck related variable
    [SerializeField] private List<Deck> decks;
    public List<Card> deck;

    // Cards in each player hand
    private List<ulong> hostCards { get; set; }
    private List<ulong> clientCards { get; set; }

    // Main Canvas UI's parts
    public Transform mainCanvas { get; private set; }
    public Transform background { get; private set; }
    public Transform playerCharacter { get; private set; }
    public Transform enemyCharacter { get; private set; }
    public Transform dropZone { get; private set; }
    public Transform dropZoneContainer { get; private set; }
    public Transform playerZone { get; private set; }
    public Transform enemyZone { get; private set; }
    public Transform playerHand { get; private set; }
    public Transform enemyHand { get; private set; }
    public TextMeshProUGUI playerHPLabel { get; private set; }
    public TextMeshProUGUI enemyHPLabel { get; private set; }

    public static WOEGameManager Instance { get; private set; }

    // Player's reference
    private PlayerNetwork hostPlayer = null;
    private PlayerNetwork clientPlayer = null;

    // For local stuff
    private void Awake()
    {
        Instance = this;

        // So this doesn't get destroyed from Lobby scene
        DontDestroyOnLoad(this);

        // Initilizing UI reference variables
        mainCanvas = GameObject.Find(MAIN_CANVAS).transform;
        background = mainCanvas.Find(BACKGROUND);
        playerCharacter = mainCanvas.Find(PLAYER_CHARACTER);
        enemyCharacter = mainCanvas.Find(ENEMY_CHARACTER);
        dropZone = mainCanvas.Find(DROP_ZONE);
        dropZoneContainer = dropZone.Find(DROP_ZONE_CONTAINER);
        playerZone = mainCanvas.Find(PLAYER_ZONE);
        enemyZone = mainCanvas.Find(ENEMY_ZONE);
        playerHand = playerZone.Find(PLAYER_HAND);
        enemyHand = enemyZone.Find(ENEMY_HAND);

        //Initilizing "player's HP" and "enemy's HP" Label
        playerHPLabel = playerCharacter.GetComponentInChildren<TextMeshProUGUI>();
        enemyHPLabel = enemyCharacter.GetComponentInChildren<TextMeshProUGUI>();
    }

    // Use instead of Start() for online stuff
    public override void OnNetworkSpawn()
    {
        // Random a deck to play
        Notify_RandomDeckServerRpc();

        // Update both players' references everytime a player has connected
        NetworkManager.OnClientConnectedCallback += (ulong playerNetworkID) =>
        {
            GetPlayerRefClientRpc();
        };

        hostCards = new List<ulong>();
        clientCards = new List<ulong>();
    }

    // ******************* Random a deck to play *******************
    [ServerRpc(RequireOwnership = false)]
    public void Notify_RandomDeckServerRpc()
    {
        // Select a *Reference* to a random Deck Scriptable Object
        randomDeckIndex.Value = UnityEngine.Random.Range(0, decks.Count);

        // Notify each client about the random deck index
        RandomDeckClientRpc(randomDeckIndex.Value);
    }

    [ClientRpc]
    public void RandomDeckClientRpc(int chosenDeckIndex)
    {
        // Copy a random deck (cards)
        // This will prevent original scriptable object's data being messed up
        deck = new List<Card>(decks[chosenDeckIndex].cards);
    }
    // ******************* Random a deck to play *******************

    // ******************* Draw card *******************
    // Client -> Server
    // Let the server handles drawing logic and we will put it in corresponding player's hand later in ClientRpc
    [ServerRpc(RequireOwnership = false)]
    public void Notify_DrawServerRpc(ServerRpcParams serverRpcParams)
    {
        // Return if there is no card left in the deck
        if (deck.Count <= 0) return;

        // The network id of the player who requested drawing
        ulong drawPlayerID = serverRpcParams.Receive.SenderClientId;

        // ******************* Template part *******************
        // Instantiate card template
        Transform cardTransform = Instantiate(cardTemplateTransform, Vector3.zero, Quaternion.identity);

        // Get card template's network object component
        NetworkObject cardNetworkObject = cardTransform.GetComponent<NetworkObject>();

        // Make spawned card appeared on the network and assigned ownership
        cardNetworkObject.SpawnWithOwnership(drawPlayerID);

        // Update corresponding player hand that is being managed by game manager
        if (drawPlayerID == 0)
        {
            hostCards.Add(cardNetworkObject.NetworkObjectId);
        }
        else if (drawPlayerID == 1)
        {
            clientCards.Add(cardNetworkObject.NetworkObjectId);
        }

        // Update card information
        DrawClientRpc(serverRpcParams.Receive.SenderClientId, cardNetworkObject.NetworkObjectId);
    }

    // Putting drawing logic in Client side will make both players draw through 1 server request
    // Which will cause unintentional result when drawing a card (Both players will draw once which is not what we wanted)
    // Server -> Clients
    // Draw a card -> Put the card in the corresponding player's hand -> remove it from the cards list (deck)
    [ClientRpc]
    private void DrawClientRpc(ulong drawPlayer, ulong objectID)
    {
        // Get spawned card network object by using passed objectID
        NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectID];

        // Get transform of the spawned card
        Transform cardTransform = cardNetworkObject.transform;

        // We are going to draw from the last card of the list
        // So we don't need to rearrange/sort every time we draw
        // Because after we remove index 0, index 1 needs to move to index 0 and so on...
        Card card = deck[deck.Count - 1];

        // ******************* Update card information part *******************
        // Dynamically update card information

        // Set card's name in editor to the actual card's name
        cardTransform.gameObject.name = card.cardName;

        // Get CardTemplate.cs component to update the card through function in the class
        CardTemplate cardTemplateComponent = cardTransform.GetComponent<CardTemplate>();

        // Update card's information according to the card player've drawn
        cardTemplateComponent.InitializeCardInformation(card.cardName, card.value.ToString(), card.cardDescription, card.cardSprite);

        // If drawing player is the same person with the one that owning the card, then put the card into player's hand
        if (NetworkManager.LocalClientId == drawPlayer)
        {
            // Set instantiated card's parent to player hand
            cardTransform.SetParent(playerHand);
        }
        // If drawing player is another player, put the card into the enemy hand
        else
        {
            // Set instantiated card's parent to enemy hand
            cardTransform.SetParent(enemyHand);

            // Also set card back cover if it is in enemy hand
            cardTemplateComponent.GetCardBackImage().gameObject.SetActive(true);

            // Hide card value (right now value's UI is out of card cover)
            cardTemplateComponent.GetCardValueTransform().gameObject.SetActive(false);
        }

        // To prevent new parent local position messing up our card local position
        cardTransform.localPosition = Vector3.zero;

        // To prevent new parent scaling messing up our card local scale
        cardTransform.localScale = Vector3.one;

        // Remove the last card (drawn card) from the deck locally (On both clients)
        Invoke(nameof(RemoveDrawnCard), 0.2f);
    }
    // ******************* Draw card *******************



    // ******************* Update player HP *******************
    // Client -> Server (Function will be called on server side but clients won't receive update)
    // So we need to call ClientRpc version to notify all clients about the update
    // Set Player's HP
    [ServerRpc(RequireOwnership = false)]
    public void Notify_SetPlayerHealthServerRpc(ulong targetID, int hp)
    {
        SetPlayerHealthClientRpc(targetID, hp);
    }

    // Server -> Clients (Client does not own server so client won't be able to call ClientRpc function directly!!)
    /// <summary> Setting corresponding player's HP (work on all clients) </summary>
    /// <param name="playerClientID_in"> Player's client ID (0 = host, 1 = client) </param>
    /// <param name="hp_in"> HP value that we want to set </param>
    [ClientRpc]
    private void SetPlayerHealthClientRpc(ulong targetID, int hp)
    {
        // If local player received dmg, then should apply change to playerHPLabel
        if(targetID == NetworkManager.LocalClientId)
        {
            playerHPLabel.text = hp.ToString();
        }
        // If target network ID is not the same with local player's, then apply change to enemyHPLabel
        else
        {
            enemyHPLabel.text = hp.ToString();
        }
    }
    // ******************* Update player HP *******************



    // ******************* Update card position *******************
    /*[ServerRpc]
    private void PlaceCardAtServerRpc(Transform placeAt)
    {
        
    }*/
    // ******************* Update card position *******************


    private void RemoveDrawnCard()
    {
        // Remove the last card from the deck
        deck.RemoveAt(deck.Count - 1);
    }



    // ******************* Get player reference *******************

    [ClientRpc]
    private void GetPlayerRefClientRpc()
    {
        // Find Host's object (local) through gameobject hierachy
        GameObject tempHost = GameObject.Find("Player0");
        // If found and could extract PlayerNetwork component from gameobject
        if(tempHost != null && tempHost.TryGetComponent<PlayerNetwork>(out PlayerNetwork hostNetwork))
        {
            hostPlayer = hostNetwork;
        }

        // Find Client's object (local) through gameobject hierachy
        GameObject tempClient = GameObject.Find("Player1");
        // If found and could extract PlayerNetwork component from gameobject
        if (tempClient != null && tempClient.TryGetComponent<PlayerNetwork>(out PlayerNetwork clientNetwork))
        {
            clientPlayer = clientNetwork;
        }
    }

    // ******************* Get player reference *******************
}
