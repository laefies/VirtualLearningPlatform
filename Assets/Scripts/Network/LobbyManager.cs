using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;

// Manages multiplayer lobby logic: authentication, lobby creation/joining, heartbeats, and transitions.
public class LobbyManager : MonoBehaviour
{
    // Singleton instance for global access
    public static LobbyManager Instance { get; private set;}

    // Key used to pass the relay code
    private static string KEY_START_GAME = "Start";

    // Events to notify other systems about lobby actions
    private event EventHandler OnLeftLobby;
    private event EventHandler<LobbyEventArgs> OnJoinedLobby;
    private event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    private event EventHandler<LobbyListChangedEventArgs> OnLobbyListChanged;

    // Custom event args classes to pass lobby data
    private class LobbyEventArgs : EventArgs {
        public Lobby lobby;
    }
    private class LobbyListChangedEventArgs : EventArgs {
        public List<Lobby> lobbyList;
    }

    // Internal timers
    private float heartbeatTimer = 15f;       // Time between heartbeat pings
    private float lobbyPollTimer = 1.1f;      // Time between lobby updates

    // The currently joined lobby
    private Lobby joinedLobby;

    private void Awake() {
        Instance = this;
    }

    void Start() {
        Authenticate();
    }

    private void Update() {
        HandleLobbyHeartbeat();  // If hosting, Keep lobby alive
        HandleLobbyPolling();    // Check for updates in joined lobby

        PrintPlayers();
    }

    // Authenticates the player anonymously to Unity Services
    private async void Authenticate()
    {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += async () => { 
            if (await AnyLobbiesExist())
                QuickJoinLobby();
            else
                CreateLobbyAndStartGame();
         };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    /*
     * --- LOBBY CREATION, JOINING, LEAVING ---
     */

    // Creates a new lobby with options and adds the player
    private async Task<Lobby> CreateLobby(string lobbyName = "Lobby", int nPlayers = 4, bool isPrivate = false)
    {
        CreateLobbyOptions options = new CreateLobbyOptions { 
            IsPrivate = isPrivate, 
            Player    = CreatePlayer(),
            Data      = new Dictionary<string, DataObject> {
                { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") }
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, nPlayers, options);
        joinedLobby = lobby;

        Debug.Log("Created Lobby!");
        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

        return joinedLobby;
    }

    // Joins a specific lobby by ID
    private async void JoinLobby(Lobby lobby) {
        JoinLobbyByIdOptions options = new JoinLobbyByIdOptions { Player = CreatePlayer() };
        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);
        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    // Leaves the current lobby
    private async void LeaveLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;
                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            } catch (LobbyServiceException e) {
                Debug.Log("Error:" + e);
            }
        }
    }

    /*
     * --- LOBBY MANAGEMENT HELPERS ---
     */

    // Sends heartbeat pings to keep the lobby alive if this player is the host
    private async void HandleLobbyHeartbeat() {
        if (IsLobbyHost()) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f) {
                heartbeatTimer = 15f;
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    // Polls the lobby state periodically to check for updates or game start
    private async void HandleLobbyPolling() {
        if (joinedLobby != null) {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f) {
                lobbyPollTimer = 2f;
                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                // Check if player is still in the lobby
                if (!IsPlayerInLobby()) {
                    joinedLobby = null;
                }
            }
        }
    }

    // Creates player metadata (currently just a random name)
    private Player CreatePlayer() {
        return new Player { 
            Data = new Dictionary<string, PlayerDataObject> {
                { "PlayerName", 
                    new PlayerDataObject(
                        PlayerDataObject.VisibilityOptions.Member, 
                        "Player" + UnityEngine.Random.Range(10, 99)
                    ) 
                }
            }
        }; 
    }

    /*
     * --- UTILITY METHODS ---
     */

    // Returns the current lobby
    private Lobby GetJoinedLobby() {
        return joinedLobby;
    }

    // Checks if this player is the host of the lobby
    private bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    // Checks if the player is still in the lobby
    private bool IsPlayerInLobby() {
        if (joinedLobby != null && joinedLobby.Players != null) {
            foreach (Player player in joinedLobby.Players) {
                if (player.Id == AuthenticationService.Instance.PlayerId) {
                    return true;
                }
            }
        }
        return false;
    }

    // Prints all players in the given lobby
    private void PrintPlayers() {
        if (joinedLobby != null) {
            string message = "Players in Lobby:";
            foreach (Player player in joinedLobby.Players) {
                message += "\n Is Host?: " + (joinedLobby.HostId == player.Id) + " Name: " + player.Data["PlayerName"].Value;
            }
            Debug.Log(message);
        }
    }

    // Checks if there are any available lobbies (for UI or matchmaking logic)
    private async Task<bool> AnyLobbiesExist()
    {
        try {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            return queryResponse.Results.Count > 0;
        } catch (LobbyServiceException e) {
            Debug.Log("Error:" + e);
            return false;
        }
    }

    /*
     * --- GAME START LOGIC ---
     */

    // Starts the game if the player is host: creates a relay and updates the lobby with its code
    private async void StartGame() {
        if (IsLobbyHost()) {
            try {
                Debug.Log("Starting a new game...");

                string relayCode = await RelayManager.Instance.CreateRelay();

                // Update the lobby to share the relay code
                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                    Data = new Dictionary<string, DataObject> {
                        { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });

                joinedLobby = lobby;
                Debug.Log("Game Started");
            } catch (LobbyServiceException e) {
                Debug.Log("Lobby Error: " + e);
            }
        }
    }

    // Create a lobby and start up the game
    private async void CreateLobbyAndStartGame() {
        await CreateLobby();
        StartGame();
    }

    // Quickly join the first available server
    private async void QuickJoinLobby() {
        try {
            // Try to join any available lobby
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions { Player = CreatePlayer() };
            joinedLobby = await Lobbies.Instance.QuickJoinLobbyAsync(options);

            // Join relay
            RelayManager.Instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
        } catch (LobbyServiceException e) {
            Debug.LogWarning("Failed to quick join a lobby: " + e);

            // If no lobbies exist, fallback and create a new one
            CreateLobbyAndStartGame();
        }
    }

    // Handle closing lobby
    private void OnApplicationQuit()
    {
        LeaveLobby();
    }

}