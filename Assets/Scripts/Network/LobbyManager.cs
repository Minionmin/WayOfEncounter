using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // Events
    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    public event EventHandler OnGameStart;

    // Data dictionary keys
    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_START_GAME = "RelayKey";

    // Player variables
    private string playerName;

    // Lobbies variables
    private Lobby joinedLobby;
    private float heartbeatTimer = 0f;
    private float lobbyUpdateTimer = 0f;

    private void Awake()
    {
        Instance = this;
    }

    //************** Engine Func **************//
    //*****************************************//

    private void Start()
    {
        // Subscribe CreateLobby() to Create Lobby button
        CustomLobbyUI.Instance.OnCreateButtonClicked += CustomLobbyUI_OnCreateButtonClicked;

        // Subscribe ListLobbies() to Refresh button
        RefreshButtonUI.Instance.OnRefreshButtonClicked += RefreshButtonUI_OnRefreshButtonClicked;

        // Subscribe StartGame() to Battle button
        JoinedLobbyUI.Instance.OnStartButtonClicked += JoinedLobbyUI_OnStartButtonClicked;

        // Logging in player to AuthenticationService and default player's name to " " just in case
        Authenticate(PlayerPrefs.GetString(PlayerPrefsKey.PLAYER_PREFS_KEY_PLAYER_NAME, " "));
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }


    //************** Lobby Network **************//
    //*******************************************//

    // Authenticate (sign in) player upon player registers their name through main menu
    private async void Authenticate(string playerName_in)
    {
        try
        {
            // Assign player name by the value receiving from main menu param
            playerName = playerName_in;

            // Initialization option
            InitializationOptions options = new InitializationOptions();
            options.SetProfile(playerName);

            // Initialize service
            await UnityServices.InitializeAsync(options);

            // Sign in event
            AuthenticationService.Instance.SignedIn += () =>
            {
                //Debug.Log($"Signed in: {playerName}  {AuthenticationService.Instance.PlayerId}");
            };

            // Player signs in anonymously
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch(AuthenticationException e)
        {
            Debug.Log(e);
        }
    }

    // Check if player is host or not
    public bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private async void CreateLobby(string lobbyName_in)
    {
        try
        {
            // Set lobby name and max player
            string lobbyName = lobbyName_in;
            const int maxPlayer = 2;

            // Lobby options
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                // Registering player name into lobby data
                Player = new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>
                {
                    { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
                }),

                // Registering default ("0") relay join code (will use it when value changed to notify when to join the game)
                Data = new Dictionary<string, DataObject>
                {
                    {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0")},
                }
            };

            // Create lobby through LobbyServices
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayer, options);

            // Make player stores variable(ref) to lobby they have created
            joinedLobby = lobby;

            // Make the host joins their own lobby
            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    // Refresh the lobby when player clicks the refresh button
    private async void RefreshLobbies()
    {
        try
        {
            // Query for lobbies
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            // Lobby List UI updating event
            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = queryResponse.Results});
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    // Join lobby upon clicking on the lobby directly
    // The clicked lobby (LobbyObjectUI) will return id and automatically join the player to the lobby
    public async void JoinLobbyById(string id_in)
    {
        try
        {
            // Assign player name upon joining certain lobby
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        {KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)},
                    }
                }
            };

            // Unity Service lobby joining process
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(id_in, options);

            joinedLobby = lobby;

            // Show and update lobby UI at JoinedLobbyUI class
            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    
    private async void StartGame()
    {
        if(IsLobbyHost())
        {
            try
            {
                Debug.Log("StartGame");

                // Create Relay
                string relayCode = await RelayManager.Instance.CreateRelay();

                // Update lobby's relay join code
                Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) },
                    }
                });

                // Update player's lobby after updated
                joinedLobby = lobby;
            }
            catch(LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    // Send heartbeat to lobby every n second to prevent lobby becoming inactive
    private async void HandleLobbyHeartbeat()
    {
        if(IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 20.0f;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }
    // Request lobby's update online every n second to sync the information between players
    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                Debug.Log(joinedLobby.Data[KEY_START_GAME].Value);

                // Sync lobby information
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                // Update lobby UI after polling
                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                joinedLobby = lobby;

                // Start game if host has joined the game (relay join code changed from 0)
                if (joinedLobby.Data[KEY_START_GAME].Value != "0")
                {
                    if(!IsLobbyHost())
                    {
                        // Join relay using the changed one
                        RelayManager.Instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                    }

                    // Cleaning joinedLobby variable
                    joinedLobby = null;

                    // Load next scene
                    OnGameStart?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    //************** UI **************//
    //********************************//

    // Create Lobby when "Create" button is clicked and set UI by OnJoinedLobby.cs Event (UpdateLobby() function)
    private void CustomLobbyUI_OnCreateButtonClicked(object sender, CustomLobbyUI.OnCreateButtonClickedEventArgs e)
    {
        CreateLobby(e.lobbyName);
    }

    // Query lobby list when refresh button is pressed
    private void RefreshButtonUI_OnRefreshButtonClicked(object sender, System.EventArgs e)
    {
        RefreshLobbies();
    }
    private void JoinedLobbyUI_OnStartButtonClicked(object sender, EventArgs e)
    {
        StartGame();
    }

    //*********** Getter-Setter ***********//
    //*************************************//
    public Lobby GetJoinedLobby()
    {
        return joinedLobby;
    }
}
