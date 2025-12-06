using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Netcode;

// Manages multiplayer lobby logic: authentication, lobby creation/joining, heartbeats, and transitions.
public class LobbyManager : MonoBehaviour
{
    // Singleton instance for global access
    public static LobbyManager Instance { get; private set;}

    // Lobby data keys
    public static string KEY_RELAY_CODE  = "RelayCode";  // Key used to pass the relay code
    public static string KEY_LOBBY_STATE = "LobbyState"; // Key used to show lobby status

    // Player data keys
    public static string KEY_NETWORK_CLIENT_ID = "NetworkClientID"; // Key used to store player network ID

    // Events to notify other systems about lobby actions
    public event EventHandler OnLeftLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyListChangedEventArgs> OnLobbyListChanged;
    public event EventHandler<LobbyEventArgs> OnGameStarted;

    // Custom event args classes to pass lobby data
    public class LobbyEventArgs : EventArgs {
        public Lobby lobby;
    }
    public class LobbyListChangedEventArgs : EventArgs {
        public List<Lobby> lobbyList;
    }

    // Internal timers
    private float heartbeatTimer = 15f;   // Time between heartbeat pings
    private float lobbyPollTimer = 1.1f;  // Time between lobby updates

    // The currently joined lobby
    private Lobby joinedLobby;
    private bool isAuthenticated;

    private void Awake() {
        Instance = this;

        // Authenticate();
    }

    async void Update() {
        if (isAuthenticated) {
            HandleLobbyHeartbeat();  // If hosting, Keep lobby alive
            HandleLobbyPolling();    // Check for updates in joined lobby
        }
    }

    // Authenticates the player anonymously to Unity Services
    public async void Authenticate()
    {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += async () => {
            isAuthenticated = true;
            RefreshLobbyList();
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    /*
     * --- LOBBY CREATION, JOINING, LEAVING ---
     */

    // Creates a new lobby with options and adds the player
    public async Task<Lobby> CreateLobby(string lobbyName = "Lobby", int nPlayers = 10, bool isPrivate = false)
    {
        if (!IsPlayerInLobby()) {
            CreateLobbyOptions options = new CreateLobbyOptions { 
                IsPrivate = isPrivate, 
                Player    = CreatePlayer(),
                Data      = new Dictionary<string, DataObject> {
                    { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member,  "0") },
                    { KEY_LOBBY_STATE, new DataObject(DataObject.VisibilityOptions.Public, "Waiting") }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, nPlayers, options);
            joinedLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });      
        }  

        return joinedLobby;
    }

    // Join a specific lobby sent as a parameter
    public async Task<Lobby> JoinLobby(Lobby lobby) {
        try {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions { Player = CreatePlayer() };

            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

        } catch (LobbyServiceException e) {

            // Usually refers to cases where the lobby doesn't exist anymore
            Debug.Log("[Lobby Error]" + e);
            RefreshLobbyList();
        }

        return joinedLobby;
    }

    // Leaves the current lobby
    public async void LeaveLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;

                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            } catch (LobbyServiceException e) {
                Debug.Log("[Lobby Error]" + e);
            }
        }
    }

    // Kicks a player from a lobby
    public async void RemoveFromLobby(string playerID) {
        if (IsLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerID);
            } catch (LobbyServiceException e) {
                Debug.Log("[Lobby Error]" + e);
            }
        }
    }

    // Transfers ownership of the lobby to another player
    public async void MigrateLobbyHost(string playerID) {
        if (IsLobbyHost()) {
            try {
                joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                    HostId = playerID
                });
            } catch (LobbyServiceException e) {
                Debug.Log("[Lobby Error]" + e);
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

        // If the player is presumably inside a lobby and still in menu scene...
        if (joinedLobby != null && !RelayManager.Instance.IsInRelay()) {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f) {
                lobbyPollTimer = 2f;

                // 1 :: Get lobby updates
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;

                // 2 :: Handle cases where player was unexpectedly removed from the lobby
                if (!IsPlayerInLobby()) {
                    OnLeftLobby?.Invoke(this, EventArgs.Empty);
                    joinedLobby = null;
                    return;
                }

                // 3 :: Check if the game has started, handling any relay needs
                if (lobby.Data[KEY_RELAY_CODE].Value != "0") {
                    // Start the game if the relay is set!
                    if (!IsLobbyHost()) { // Host has already joined
                        // Join relay and network, which automatically syncs network scenes and objects
                        RelayManager.Instance.JoinRelay(lobby.Data[KEY_RELAY_CODE].Value);
                        OnGameStarted?.Invoke(this, new LobbyEventArgs { lobby = lobby });
                        return;
                    } 
                }
                
                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = lobby });
            }
        }
    }

    // Fetches the list of existing lobbies
    public async void RefreshLobbyList() {
        try {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filters -
            //  :: Find only open lobbies (lobbies with open slots)
            options.Filters = new List<QueryFilter> {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            //  :: Order results showing newer lobbies first
            options.Order = new List<QueryOrder> {
                new QueryOrder( 
                    asc: false, 
                    field: QueryOrder.FieldOptions.Created)
            };

            // Query for the list and invoke the event to signal new values
            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            OnLobbyListChanged?.Invoke(this, new LobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        } catch(LobbyServiceException e) {
            Debug.Log("[Lobby Error]" + e);
        }
    }

    /*
     * --- PLAYER METHODS ---
     */

    // Creates player metadata (just a random name)
    private Player CreatePlayer() {
        return new Player { 
            Data = new Dictionary<string, PlayerDataObject> {
                { "PlayerName", 
                    new PlayerDataObject(
                        PlayerDataObject.VisibilityOptions.Member, 
                        "Player" + UnityEngine.Random.Range(10, 99)
                    ) 
                },
                { KEY_NETWORK_CLIENT_ID,  
                  new PlayerDataObject( PlayerDataObject.VisibilityOptions.Member, "Not Connected") }
            }
        }; 
    }


    /*
     * --- UTILITY METHODS ---
     */

    // Checks if this player is the host of the lobby
    public bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    // Checks if the player is still in the lobby
    public bool IsPlayerInLobby() {
        if (joinedLobby != null && joinedLobby.Players != null) {
            foreach (Player player in joinedLobby.Players) {
                if (player.Id == AuthenticationService.Instance.PlayerId) {
                    return true;
                }
            }
        }
        return false;
    }

    /*
     * --- GAME LOGIC ---
     */

    // Starts the game if the player is host: creates a relay and updates the lobby with its code
    public async void StartGame() {
        try {
            string relayCode = await RelayManager.Instance.CreateRelay();

            Lobby lobby = null;

            // If in a lobby, information regarding the relay code must be 
            // shared across all players, so they can join the relay and server
            if (IsLobbyHost()) {
                lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                    Data = new Dictionary<string, DataObject> {
                        { KEY_RELAY_CODE,  new DataObject(DataObject.VisibilityOptions.Member, relayCode) },
                        { KEY_LOBBY_STATE, new DataObject(DataObject.VisibilityOptions.Public, "Ingame") }
                    }
                });

                joinedLobby = lobby;
            }

            // Signal game started event, in order to transition into the correct scene
            OnGameStarted?.Invoke(this, new LobbyEventArgs { lobby = lobby });
            
        } catch (LobbyServiceException e) {
            Debug.Log("[Lobby Error]" + e);
        }
    }
}