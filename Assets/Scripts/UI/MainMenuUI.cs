using System;
using System.Collections.Generic;
using System.Collections;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

// Manages UI to handle lobby events.
public class MainMenuUI : FollowPlayerUI
{
    // Messages » Headers / Info Subheaders
    private const string GAME_MENU_HEADER_STARTABLE   = "Choose a Lesson!";
    private const string GAME_MENU_HEADER_UNSTARTABLE = "Waiting for Host!";

    private const string GAME_MENU_SUBHEADER_SOLO     = "You're not in a lobby! Pick a lesson to explore on your own.";
    private const string GAME_MENU_SUBHEADER_NON_HOST = "Waiting for host! Meanwhile, feel free to browse the list.";
    private const string GAME_MENU_SUBHEADER_HOST     = "Choose a lesson - everyone in the lobby will join you!";

    private Color[] mainListColors = new Color[] {
        new Color(0.639f, 0.741f, 0.922f), // ≈ #A3BDEB
        new Color(0.945f, 0.808f, 0.620f), // ≈ #F1CE9E
        new Color(0.933f, 0.667f, 0.384f)  // ≈ #EDA95F
    };

    private Color[] accentListColors = new Color[] {
        new Color(0.451f, 0.604f, 0.820f), //≈ #739AD1
        new Color(0.933f, 0.706f, 0.467f), //≈ #EEB477
        new Color(0.851f, 0.510f, 0.380f)  //≈ #d98261
    };

    // Game Menu - Game listing + starting
    [Header("Game Menu Components")]
    [SerializeField] private TMPro.TextMeshProUGUI gameMenuHeader;
    [SerializeField] private TMPro.TextMeshProUGUI gameMenuSubheader;
    [SerializeField] private Button startButton;

    // Lobby Menu - Lobby creation + listing 
    [Header("Lobby Menu Components")]
    [SerializeField] private GameObject lobbyMenu;       
    [SerializeField] private Transform lobbyListContent; // Content panel where the lobbies are listed
    [SerializeField] private GameObject lobbyItemPrefab; // Prefab regarding an item of the lobby list

    // Player Menu - Lobby waiting list 
    [Header("Player Menu Components")]
    [SerializeField] private GameObject playerList;
    [SerializeField] private Transform playerListContent; // Content panel where the players are listed
    [SerializeField] private GameObject playerItemPrefab; // Prefab regarding an item of the player list

    void Start() {
        // Call base initialization
        base.Start();

        // Prepare Visualization
        ClearLobbyItems();
        ClearPlayerItems();
        HandleMenuVisibility();

        // Subscribe to Lobby related events
        LobbyManager.Instance.OnLobbyListRefreshed  += UpdateLobbyList;
        LobbyManager.Instance.OnLobbyPlayersChanged += HandleJoinedLobbyUpdate;
        LobbyManager.Instance.OnLobbyJoined         += HandleJoinedLobby;
        LobbyManager.Instance.OnLobbyLeft           += HandleLeftLobby;
    }

    void OnDestroy() {
        // Unsubscribe from Lobby related events
        LobbyManager.Instance.OnLobbyListRefreshed  -= UpdateLobbyList;
        LobbyManager.Instance.OnLobbyPlayersChanged -= HandleJoinedLobbyUpdate;
        LobbyManager.Instance.OnLobbyJoined       -= HandleJoinedLobby;
        LobbyManager.Instance.OnLobbyLeft         -= HandleLeftLobby;

        // Clean up any remaining lobby & player items
        ClearLobbyItems();
        ClearPlayerItems();
    }

    // Swap visible menu & call header/subheader handling method for the main game menu
    void HandleMenuVisibility() {
        bool inLobby = LobbyManager.Instance.IsInLobby;
        lobbyMenu.SetActive(!inLobby);    // Not in a lobby? » Show available lobbies list
        playerList.SetActive(inLobby);    //     in a lobby? » Show players in the current lobby 

        HandleGameMenuState();
    }

    /*
     * --- HANDLE MENU FLOW ---
     */

    // Called when a lobby is created or picked from the lobby list
    void HandleJoinedLobby(Lobby lobby) {
        HandleMenuVisibility();      // Swap menus to show player list;
        UpdatePlayerList(lobby.Players);   // Show lobby players on screen; 
        ClearLobbyItems();           // Clear lobby list as its not visible;
    }

    // Called when a lobby is left
    void HandleLeftLobby() {
        HandleMenuVisibility();      // Swap menus to show lobby list;
        RefreshLobbies();            // Show existing lobbies on screen;
        ClearPlayerItems();          // Clear player list as its not visible;
    }

    // Continuously called while in a lobby
    void HandleJoinedLobbyUpdate(List<Player> players) {
        UpdatePlayerList(players);   // Update lobby information through all menus
        HandleGameMenuState();       // Swap header/subheader messages
    }

    /*
     * --- LOBBY LISTING MENU ---
     */

    // Visually updates the list of lobbies the player can join
    void UpdateLobbyList(List<Lobby> lobbyList) {
        // Clear existing lobby items first
        ClearLobbyItems();
        
        // Create new lobby items for each lobby in the list
        for (int i = 0; i < lobbyList.Count; i++)
        {
            Lobby lobby = lobbyList[i];

            if (lobby != null) {
                // Instantiate the lobby item prefab as a child of the content panel
                GameObject lobbyItemGO = Instantiate(lobbyItemPrefab, lobbyListContent);
                
                // Assign the lobby data to the list item
                LobbyListItemUI lobbyItem = lobbyItemGO.GetComponent<LobbyListItemUI>();
                lobbyItem.SetLobby(lobby);

                // Cycle through the possible colors meaning: 0,1,2,0,1,2,...
                lobbyItem.SetColor(mainListColors[i % mainListColors.Length], 
                                accentListColors[i % accentListColors.Length]);             
            }
        }
    }

    // Clears all lobby items from view
    void ClearLobbyItems() {
        foreach (Transform child in lobbyListContent.transform) Destroy(child.gameObject);
    }

    /*
     * --- WAITING ROOM MENU ---
     */
    
    // Visually updates header and subheader of the game menu, and handles button interaction
    void HandleGameMenuState() {
        // Check if the player is in a lobby
        bool inLobby = LobbyManager.Instance.IsInLobby;

        gameMenuHeader.text    = (!inLobby || LobbyManager.Instance.IsHost) ? GAME_MENU_HEADER_STARTABLE 
                                                                                   : GAME_MENU_HEADER_UNSTARTABLE;
        gameMenuSubheader.text = (!inLobby) ? GAME_MENU_SUBHEADER_SOLO 
                                            : ( LobbyManager.Instance.IsHost ? GAME_MENU_SUBHEADER_HOST
                                                                                      : GAME_MENU_SUBHEADER_NON_HOST );
        // A game can only be started if:
        //   1. User is playing alone    2. User is hosting the lobby they are in
        startButton.IsInteractable = !inLobby || LobbyManager.Instance.IsHost;
    }

    /*
     * --- PLAYER MENU ---
     */

    // Visually updates the list of players in the lobby
    void UpdatePlayerList(List<Player> players) {
        // Clear existing player items first
        ClearPlayerItems();

        // Create new player items for each player in the list
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];

            if (player != null) {
                // Instantiate the player item prefab as a child of the content panel
                GameObject playerItemGO = Instantiate(playerItemPrefab, playerListContent);
                
                // Assign the player data to the list item
                PlayerListItemUI playerItem = playerItemGO.GetComponent<PlayerListItemUI>();
                playerItem.SetPlayer(player, LobbyManager.Instance.IsPlayerHost(player.Id)); // Checks if hosting

                // Cycle through the possible colors meaning: 0,1,2,0,1,2,...
                playerItem.SetColor(mainListColors[i % mainListColors.Length], 
                                accentListColors[i % accentListColors.Length]);            
            } 
        }
    }

    // Clears all lobby items from view
    void ClearPlayerItems() {
        foreach (Transform child in playerListContent.transform) Destroy(child.gameObject);
    }


    /*
     * --- HANDLE INTERACTION ---
     */

    // Called by clicking the "refresh" button, to update lobby list
    public async void RefreshLobbies() {
        await LobbyManager.Instance.RefreshLobbyListAsync();
    }

    // Called by clicking the "+" button, to create a new lobby
    public async void CreateLobby() {
        await LobbyManager.Instance.CreateLobbyAsync(PlayerManager.Instance.PlayerName);
    }

    // Called by clicking the "back" button, to return to lobby list
    public async void LeaveLobby() {
        LobbyManager.Instance.LeaveLobbyAsync();
    }

    // Called by clicking the "start" button, to start the chosen game
    public async void StartGame() {
        if (!LobbyManager.Instance.IsInLobby || LobbyManager.Instance.IsHost) {
            await LobbyManager.Instance.StartExperienceAsync();
        }
    }

    void Update() {
        base.Update();

        if (Input.GetKeyDown(KeyCode.N)) CreateLobby();
        if (Input.GetKeyDown(KeyCode.R)) RefreshLobbies();
        if (Input.GetKeyDown(KeyCode.L)) LeaveLobby();
        if (Input.GetKeyDown(KeyCode.G)) StartGame();
    }

}