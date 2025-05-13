using System;
using System.Collections.Generic;
using System.Collections;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// Manages UI to handle lobby events.
public class MainMenuUI : BodyLockedUI
{

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


    // First Menu - Lobby creation + listing 
    [SerializeField] private GameObject lobbyMenu;       
    [SerializeField] private Transform lobbyListContent; // Content panel where the lobbies are listed
    [SerializeField] private GameObject lobbyItemPrefab; // Prefab regarding an item of the lobby list

    // Second Menu - Lobby waiting list 
    [SerializeField] private GameObject waitingRoom;
    [SerializeField] private GameObject playerList;
    [SerializeField] private Transform playerListContent; // Content panel where the players are listed
    [SerializeField] private GameObject playerItemPrefab; // Prefab regarding an item of the player list

    [SerializeField] private Button startButton;

    void Start() {
        // Call base initialization
        base.Start();

        // Prepare Visualization
        ClearLobbyItems();
        ClearPlayerItems();
        HandleMenuVisibility();

        // Subscribe to Lobby related events
        LobbyManager.Instance.OnLobbyListChanged  += UpdateLobbyList;
        LobbyManager.Instance.OnJoinedLobbyUpdate += UpdateWaitingRoom;
        LobbyManager.Instance.OnJoinedLobby       += HandleJoinedLobby;
        LobbyManager.Instance.OnLeftLobby         += HandleLeftLobby;

        // Authenticate into Unity Services
        LobbyManager.Instance.Authenticate();
    }

    void OnDestroy() {
        // Unsubscribe from Lobby related events
        LobbyManager.Instance.OnLobbyListChanged  -= UpdateLobbyList;
        LobbyManager.Instance.OnJoinedLobbyUpdate -= UpdateWaitingRoom;
        LobbyManager.Instance.OnJoinedLobby       -= HandleJoinedLobby;
        LobbyManager.Instance.OnLeftLobby         -= HandleLeftLobby;

        // Clean up any remaining lobby items
        ClearLobbyItems();
        ClearPlayerItems();
    }

    // Swap visible menu
    void HandleMenuVisibility() {
        bool inLobby = LobbyManager.Instance.IsPlayerInLobby();
        lobbyMenu.SetActive(!inLobby);
        waitingRoom.SetActive(inLobby);
        playerList.SetActive(inLobby);
    }

    /*
     * --- HANDLE MENU FLOW ---
     */

    // After creating a lobby or picking from the list, direct to waiting room interface
    void HandleJoinedLobby(object sender, LobbyManager.LobbyEventArgs e) {
        HandleMenuVisibility();
    }

    // If leaving a lobby, direct back to lobby choice room interface
    void HandleLeftLobby(object sender, EventArgs e) {
        HandleMenuVisibility();
        ClearPlayerItems();
        RefreshLobbies();
    }

    /*
     * --- LOBBY LISTING MENU ---
     */

    // Visually updates the list of lobbies the player can join
    void UpdateLobbyList(object sender, LobbyManager.LobbyListChangedEventArgs e) {
        // Clear existing lobby items first
        ClearLobbyItems();
        
        // Create new lobby items for each lobby in the list
        for (int i = 0; i < e.lobbyList.Count; i++)
        {
            Lobby lobby = e.lobbyList[i];

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

    // Clears all lobby items from view
    void ClearLobbyItems() {
        foreach (Transform child in lobbyListContent.transform) Destroy(child.gameObject);
    }

    /*
     * --- WAITING ROOM MENU ---
     */

    // Enables visualization of information regarding joined lobby
    void UpdateWaitingRoom(object sender, LobbyManager.LobbyEventArgs e) {
        Lobby lobby = e.lobby; 

        // Update the list of players in the same lobby
        UpdatePlayerList(lobby);

        // Game Selection
        
        // A game can only be started by the host
        startButton.interactable = LobbyManager.Instance.IsLobbyHost();
    }

    // Visually updates the list of players in the lobby
    void UpdatePlayerList(Lobby lobby) {
        // Clear existing player items first
        ClearPlayerItems();

        // Create new player items for each player in the list
        for (int i = 0; i < lobby.Players.Count; i++)
        {
            Player player = lobby.Players[i];

            // Instantiate the player item prefab as a child of the content panel
            GameObject playerItemGO = Instantiate(playerItemPrefab, playerListContent);
            
            // Assign the player data to the list item
            PlayerListItemUI playerItem = playerItemGO.GetComponent<PlayerListItemUI>();
            playerItem.SetPlayer(player, lobby.HostId == player.Id); // Checks if hosting

            // Cycle through the possible colors meaning: 0,1,2,0,1,2,...
            playerItem.SetColor(mainListColors[i % mainListColors.Length], 
                              accentListColors[i % accentListColors.Length]);            
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
    public void RefreshLobbies() {
        LobbyManager.Instance.RefreshLobbyList();
    }

    // Called by clicking the "+" button, to create a new lobby
    public async void CreateLobby() {
        await LobbyManager.Instance.CreateLobby("Lobby" + UnityEngine.Random.Range(10, 99));
    }

    // Called by clicking the "back" button, to return to lobby list
    public void LeaveLobby() {
        LobbyManager.Instance.LeaveLobby();
    }

    // Called by clicking the "start" button, to start the chosen game
    public void StartGame() {
        LobbyManager.Instance.StartGame();
    }

    // TODO REMOVE AFTER TESTING
    void Update() {
        base.Update();

        if (Input.GetKeyDown(KeyCode.N)) CreateLobby();
        if (Input.GetKeyDown(KeyCode.R)) RefreshLobbies();
        if (Input.GetKeyDown(KeyCode.L)) LeaveLobby();
        if (Input.GetKeyDown(KeyCode.S)) StartGame();
    }

}