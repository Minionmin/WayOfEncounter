/**********************************************
 * 
 *  WOEGameManager.cs 
 *  Game state manager
 * 
 *  製作者：Phansuwan Chaichumphon （ミン）
 * 
 **********************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;
using static Card;

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

    // Events
    public event EventHandler OnGameStateChange;

    // Template variable
    [Header("Deck/Card template")]
    [SerializeField] private Transform cardTemplateTransform;

    // Network variable
    /// <summary> Index of the deck we got from randomization </summary>
    NetworkVariable<int> randomDeckIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary> The attack token determine which player's going to attack (0 = host attack, 1 = client attack) </summary>
    NetworkVariable<int> attackToken = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary> Number of turn that has been passed </summary>
    // NetworkVariable<int> currentTurn = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Deck related variable
    /// <summary> This is the original list of decks that are directly referenced from ScriptableObject </summary>
    [SerializeField] private List<Deck> decks;
    /// <summary> This is the deck that we got from randomizing (the copy version, not the original (not the ScriptableObject one)) </summary>
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

    public enum GameState
    {
        // At the very first stage of the match
        NetworkPreparing,
        // Initialization stuff goes here
        StandBy,
        // Client puts the card into the dropZone
        HostTurn,
        // Client puts the card into the dropZone
        ClientTurn,
        // Process the outcome of cards in the dropZone
        ProcessTurn
    }

    // Game state that will run the game
    public GameState state = GameState.NetworkPreparing;

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

        // Initilizing "player's HP" and "enemy's HP" Label
        playerHPLabel = playerCharacter.GetComponentInChildren<TextMeshProUGUI>();
        enemyHPLabel = enemyCharacter.GetComponentInChildren<TextMeshProUGUI>();

        // Initializing host, client cards list
        hostCards = new List<ulong>();
        clientCards = new List<ulong>();

        // Register confirm function to ActionUI's confirm button
        ActionUI.Instance.OnConfirmButtonClicked += ActionUI_OnConfirmButtonClicked;
        // We initialize actionUI here just in case WOEManager is not spawned yet
        ActionUI.Instance.Initialize();
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
    }

    // When comfirm button is clicked, submit and process cards in the drop zone
    private void ActionUI_OnConfirmButtonClicked(object sender, System.EventArgs e)
    {
        // If it is not our turn then do nothing
        if (!IsPlayerTurn()) return;

        Notify_ConfirmTurnServerRpc(new ServerRpcParams());
    }



    // ******************* Game state transition *******************
    [ServerRpc(RequireOwnership = false)]
    public void Notify_ChangeStateToServerRpc(GameState targetState)
    {
        ChangeStateToClientRpc(targetState);
    }

    [ClientRpc]
    public void ChangeStateToClientRpc(GameState targetState)
    {
        state = targetState;
        OnGameStateChange?.Invoke(this, EventArgs.Empty);
    }
    // ******************* Game state transition *******************



    // ******************* Submit/Process cards in the drop zone *******************
    [ServerRpc(RequireOwnership = false)]
    private void Notify_ConfirmTurnServerRpc(ServerRpcParams serverRpcParams)
    {
        // Get ID of the player who pressed confirm button
        ulong confirmPlayerID = serverRpcParams.Receive.SenderClientId;

        switch(state)
        {
            case GameState.HostTurn:
                ChangeStateToClientRpc(GameState.ClientTurn);
                break;
            case GameState.ClientTurn:
                ChangeStateToClientRpc(GameState.ProcessTurn);
                Notify_ProcessTurnServerRpc();
                break;
            default: 
                break;
        }
        // The data processing is only needed on the server side so there is no point in creating ClientRpc version
    }

    // ************************************** Process

    [ServerRpc(RequireOwnership = false)]
    private void Notify_ProcessTurnServerRpc()
    {
        // If there is no card in the dropzone then we proceed to the next turn
        if (dropZoneContainer.childCount == 0) return;

        // Initializing card1 and card2
        CardTemplate card1 = null;
        CardTemplate card2 = null;

        // If there is only 1 card
        if (dropZoneContainer.childCount == 1)
        {
            card1 = dropZoneContainer.GetChild(0).GetComponent<CardTemplate>();

            // If it is any attack-type card
            if (IsAttackCard(card1))
            {
                // Attack
                Notify_AttackServerRpc(card1.gameObject.GetComponent<NetworkObject>().OwnerClientId, card1.GetCardValue(), (int)card1.GetCardType());
            }

            // Destroy the only card after it has been used
            Notify_DeleteCardServerRpc(card1.gameObject); // card1 only
        }

        // If there are 2 cards
        if (dropZoneContainer.childCount == 2)
        {
            card1 = dropZoneContainer.GetChild(0).GetComponent<CardTemplate>();
            card2 = dropZoneContainer.GetChild(1).GetComponent<CardTemplate>();

            // If the first card is attack card
            if (IsAttackCard(card1))
            {
                // If it is blockable then blocking player receives no damage
                if (IsBlockable(card1, card2))
                {
                    // Will play block effect here
                    Debug.Log($"{card1.GetCardName()} is blocked with {card2.GetCardType()}");
                }
                else
                {
                    // The first card is attack card and the attacked player didn't block
                    // Attack
                    Notify_AttackServerRpc(card1.gameObject.GetComponent<NetworkObject>().OwnerClientId, card1.GetCardValue(), (int)card1.GetCardType());
                }
            }

            // Delete cards after they have been used
            // There will be only 2 cards so we won't use loop here
            // Index will be changed after we deleted the card so becareful
            Notify_DeleteCardServerRpc(card2.gameObject); // card2
            Notify_DeleteCardServerRpc(card1.gameObject); // card1
        }

        // Todo: Make attack token worked
        ChangeStateToClientRpc(GameState.HostTurn);
    }

    // ************************************** Process

    // ************************************** Attack

    // Passing cardType by int so the code is easier to read (doesn't need to take care of custom type(Card.cardType))
    [ServerRpc(RequireOwnership = false)]
    private void Notify_AttackServerRpc(ulong attackerID, int attackValue, int damageType)
    {
        // If host attacks
        if (attackerID == hostPlayer.GetPlayerNetworkID())
        {
            Notify_SetPlayerHealthServerRpc(clientPlayer.GetPlayerNetworkID(), clientPlayer.GetPlayerHP() - attackValue);
        }
        // If client attacks
        else
        {
            Notify_SetPlayerHealthServerRpc(hostPlayer.GetPlayerNetworkID(), hostPlayer.GetPlayerHP() - attackValue);
        }

        // The data processing is only needed on the server side so there is no point in creating ClientRpc version
    }

    // ************************************** Attack

    // ******************* Submit/Process cards in the drop zone *******************



    // ******************* Draw card *******************
    // Client -> Server
    // Let the server handles drawing logic and we will put it in corresponding player's hand later in ClientRpc
    [ServerRpc(RequireOwnership = false)]
    public void Notify_DrawServerRpc(ulong drawPlayerID)
    {
        // Return if there is no card left in the deck
        if (deck.Count <= 0) return;

        // ******************* Template part *******************
        // Instantiate card template
        Transform cardTransform = Instantiate(cardTemplateTransform, Vector3.zero, Quaternion.identity);

        // Get card template's network object component
        NetworkObject cardNetworkObject = cardTransform.GetComponent<NetworkObject>();

        // Make spawned card appeared on the network and assigned ownership
        cardNetworkObject.SpawnWithOwnership(drawPlayerID);

        // Update card information
        DrawClientRpc(drawPlayerID, cardNetworkObject.NetworkObjectId);
    }

    // Putting drawing logic in Client side will make both players draw through 1 server request
    // Which will cause unintentional result when drawing a card (Both players will draw once which is not what we wanted)
    // Server -> Clients
    // Draw a card -> Put the card in the corresponding player's hand -> remove it from the cards list (deck)
    [ClientRpc]
    private void DrawClientRpc(ulong drawPlayerID, ulong objectID)
    {
        // Get spawned card network object by using passed objectID
        NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectID];

        // Get transform of the spawned card
        Transform cardTransform = cardNetworkObject.transform;

        // Update corresponding player hand that is being managed by game manager
        if (drawPlayerID == hostPlayer.GetPlayerNetworkID())
        {
            hostCards.Add(cardNetworkObject.NetworkObjectId);
        }
        else
        {
            clientCards.Add(cardNetworkObject.NetworkObjectId);
        }

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
        cardTemplateComponent.InitializeCardInformation(card.cardName, card.value.ToString(), card.cardDescription, card.cardSprite, card.cardType);

        // If drawing player is the same person with the one that owning the card, then put the card into player's hand
        if (NetworkManager.LocalClientId == drawPlayerID)
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



    // ******************* Draw multiple cards *******************
    private IEnumerator DrawMultipleCards(int numberOfCardToDraw, float timeBetweenCard, ulong playerID)
    {
        // Check if there is enough card to draw
        // If not then draw until there is no card left
        if (numberOfCardToDraw > deck.Count) numberOfCardToDraw = deck.Count;

        // Draw card every n second (prevent drawing too fast which will cause unintentional error)
        for(int i = 0; i < numberOfCardToDraw; i++)
        {
            yield return new WaitForSeconds(timeBetweenCard);

            Notify_DrawServerRpc(playerID);
        }
    }

    //************************************** Initialize player hand
    [ServerRpc(RequireOwnership = false)]
    private void Notify_InitializingPlayersHandServerRpc(int numberOfCardToDraw, float timeBetweenCard)
    {
        StartCoroutine(InitializePlayersHand(numberOfCardToDraw, timeBetweenCard));
    }

    private IEnumerator InitializePlayersHand(int numberOfCardToDraw, float timeBetweenCard)
    {
        // Draw cards for host first
        StartCoroutine(DrawMultipleCards(numberOfCardToDraw, timeBetweenCard, hostPlayer.GetPlayerNetworkID()));
        
        // Wait for each card to be drawn
        yield return new WaitForSeconds(timeBetweenCard * numberOfCardToDraw + 0.5f);

        // Then draw cards for client
        StartCoroutine(DrawMultipleCards(numberOfCardToDraw, timeBetweenCard, clientPlayer.GetPlayerNetworkID()));
    }
    //************************************** Initialize player hand

    // ******************* Draw multiple cards *******************



    // ******************* Delete a card *******************
    // Since we already know parent of the cards, there is no need to pass their network IDs
    // And we cannot pass Gameobject as parameter in RPC function
    [ServerRpc(RequireOwnership = false)]
    private void Notify_DeleteCardServerRpc(NetworkObjectReference cardNetworkReference)
    {
        // Delete the card on both clients
        if (cardNetworkReference.TryGet(out NetworkObject cardNetworkObject))
        {
            cardNetworkObject.Despawn();
        }
        //DeleteCardClientRpc(cardNetworkReference);
    }

    [ClientRpc]
    private void DeleteCardClientRpc(NetworkObjectReference cardNetworkReference)
    {

    }
    // ******************* Draw multiple cards *******************

    // ******************* Update player HP *******************
    // Client -> Server (Function will be called on server side but clients won't receive update)
    // So we need to call ClientRpc version to notify all clients about the update
    // Set Player's HP
    [ServerRpc(RequireOwnership = false)]
    public void Notify_SetPlayerHealthServerRpc(ulong targetID, int hp)
    {
        // Update Data
        // If hp update target is host
        if(hostPlayer.GetPlayerNetworkID() == targetID)
        {
            // Update host hp data
            hostPlayer.SetPlayerHP(hp);
        }
        else
        {
            // Update client hp data
            clientPlayer.SetPlayerHP(hp);
        }

        SetPlayerHealthClientRpc(targetID, hp);
    }

    // Server -> Clients (Client does not own server so client won't be able to call ClientRpc function directly!!)
    /// <summary> Setting corresponding player's HP (work on all clients) </summary>
    /// <param name="playerClientID_in"> Player's client ID (0 = host, 1 = client) </param>
    /// <param name="hp_in"> HP value that we want to set </param>
    [ClientRpc]
    private void SetPlayerHealthClientRpc(ulong targetID, int hp)
    {
        // Update UI
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



    // ******************* Update card position (Dropzone) *******************
    [ServerRpc(RequireOwnership = false)]
    public void Notify_PlaceCardAtServerRpc(ulong cardID, NetworkObjectReference placeAt)
    {
        PlaceCardAtClientRpc(cardID, placeAt);
    }

    [ClientRpc]
    private void PlaceCardAtClientRpc(ulong cardID, NetworkObjectReference placeAt)
    {
        // Check if the target place is available by using TryGet
        if (placeAt.TryGet(out NetworkObject placeObject))
        {
            Transform targetPlace = placeObject.transform;

            if (targetPlace != null)
            {
                NetworkObject cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardID];
                if (cardNetworkObject != null)
                {
                    cardNetworkObject.transform.SetParent(targetPlace, false);
                }
            }
        }
    }
    // ******************* Update card position (Dropzone) *******************



    // ******************* Update card position (Player's hand) *******************
    [ServerRpc(RequireOwnership = false)]
    public void Notify_ReturnCardToPlayerServerRpc(ulong cardID)
    {
        ReturnCardToPlayerClientRpc(cardID);
    }

    [ClientRpc]
    private void ReturnCardToPlayerClientRpc(ulong cardID)
    {
        var cardNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardID];

        // Check if this player is owning the card
        if (cardNetworkObject.OwnerClientId == NetworkManager.LocalClientId)
        {
            // Place the card in the player hand if player is owning the card
            PlaceCardAtClientRpc(cardID, playerHand.gameObject);
        }
        // If player is not owning the card
        else
        {
            // Place the card in the enemy hand instead
            PlaceCardAtClientRpc(cardID, enemyHand.gameObject);
        }

        // At last, we return the card to the original sibling index
        cardNetworkObject.transform.SetSiblingIndex(cardNetworkObject.GetComponent<DragDrop>().cardOrderInHand);
    }
    // ******************* Update card position (Player's hand) *******************



    // ******************* Random a deck to play *******************
    [ServerRpc(RequireOwnership = false)]
    public void Notify_RandomDeckServerRpc()
    {
        // Select a *Reference* to a random Deck Scriptable Object
        randomDeckIndex.Value = UnityEngine.Random.Range(0, decks.Count);

        // Notify each client about the random deck index
        RandomDeckClientRpc(randomDeckIndex.Value);

        Notify_ShuffleDeckServerRpc();
    }

    [ClientRpc]
    public void RandomDeckClientRpc(int chosenDeckIndex)
    {
        // Copy a random deck (cards)
        // This will prevent original scriptable object's data being messed up
        deck = new List<Card>(decks[chosenDeckIndex].cards);
    }
    // ******************* Random a deck to play *******************



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

        // If both players' references have been set
        if(hostPlayer && clientPlayer)
        {
            // Initialize game
            InitializeMatch();
        }
    }
    // ******************* Get player reference *******************



    // ******************* Initializing the match *******************
    // After every player is connected
    private void InitializeMatch()
    {
        // Set each player HP to maximum
        Notify_SetPlayerHealthServerRpc(hostPlayer.GetPlayerNetworkID(), hostPlayer.GetPlayerMaxHP());
        Notify_SetPlayerHealthServerRpc(clientPlayer.GetPlayerNetworkID(), clientPlayer.GetPlayerMaxHP());

        // Change to standby phase once both players are connected
        ChangeStateToClientRpc(GameState.StandBy);

        // Let host runs initialization command
        if (IsHost)
        {
            // Then draw players cards until their hand size is 5
            int numberOfCardToDraw = 5;
            float timeBetweenEachDraw = 0.4f;

            // Draw cards for host first then client
            Notify_InitializingPlayersHandServerRpc(numberOfCardToDraw, timeBetweenEachDraw);
        }

        // Todo: make attack token worked
        // Change to attacker's turn once both players' hands are initialized
        ChangeStateToClientRpc(GameState.HostTurn);
    }
    // ******************* Initializing the match *******************



    // ******************* Shuffle the deck on both clients *******************
    [ServerRpc(RequireOwnership = false)]
    private void Notify_ShuffleDeckServerRpc()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            // Firstly, we let the server randomizes the index and then pass it to clients so they have the same randomized index
            int rand = UnityEngine.Random.Range(i, deck.Count);
            // Then proceed to swap the card position on both clients (which is passed with the same index param)
            SwapCardPositionClientRpc(i, rand);
        }
    }
    // ******************* Shuffle the deck on both clients *******************



    // ******************* Swap card position on both clients *******************
    [ClientRpc]
    private void SwapCardPositionClientRpc(int i, int index)
    {
        // Swap card position
        Card temp = deck[i];
        deck[i] = deck[index];
        deck[index] = temp;
    }
    // ******************* Swap card position on both clients *******************



    // Remove the last card from the deck
    private void RemoveDrawnCard()
    {
        deck.RemoveAt(deck.Count - 1);
    }

    // Check if it is player's turn or not
    public bool IsPlayerTurn()
    {
        return state == GameState.HostTurn && NetworkManager.Singleton.LocalClientId == hostPlayer.GetPlayerNetworkID() ||
            state == GameState.ClientTurn && NetworkManager.Singleton.LocalClientId == clientPlayer.GetPlayerNetworkID();
    }

    private bool IsAttackCard(CardTemplate card1)
    {
        return card1.GetCardType() == Card.CardType.PhysicalDamage || 
            card1.GetCardType() == Card.CardType.MagicalDamage;
    }

    private bool IsBlockable(CardTemplate card1, CardTemplate card2)
    {
        return card1.GetCardType() == Card.CardType.PhysicalDamage && card2.GetCardType() == Card.CardType.PhysicalBlock ||
            card1.GetCardType() == Card.CardType.MagicalDamage && card2.GetCardType() == Card.CardType.MagicalBlock;
    }

    public bool IsInDropZoneContainer(Transform cardTransform)
    {
        return cardTransform.parent == dropZoneContainer;
    }

    public PlayerNetwork GetHostPlayer()
    {
        return hostPlayer;
    }

    private PlayerNetwork GetClientPlayer()
    {
        return clientPlayer;
    }
}