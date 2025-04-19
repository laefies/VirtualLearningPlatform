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

/* TODO
*     . Refactor game start;
*/

// Manages multiplayer lobby logic: authentication, lobby creation/joining, heartbeats, and transitions.
public class LobbyManager : MonoBehaviour
{
    // Singleton instance for global access
    public static LobbyManager Instance { get; private set;}

    // Key used to pass the relay code
    private static string KEY_START_GAME = "Start";
    public static string KEY_NETWORK_CLIENT_ID = "NetworkClientID";

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

    async void Update() {
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

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

        Debug.Log("Created a new Lobby!");
        return joinedLobby;
    }

    // Joins a specific lobby by ID
    private async void JoinLobbyByID(Lobby lobby) {
        JoinLobbyByIdOptions options = new JoinLobbyByIdOptions { Player = CreatePlayer() };
        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);
        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    // Leaves the current lobby
    public async void LeaveLobby() {
        if (joinedLobby != null) {
            try {
                Debug.Log("Leaving lobby...");
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;
                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            } catch (LobbyServiceException e) {
                Debug.Log("Error:" + e);
            }
        }
    }

    // Quicks player from a lobby
    public async void RemoveFromLobby(string playerID) {
        if (joinedLobby != null) {
            try {
                Debug.Log("Removing player from lobby...");
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerID);
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

                // Get lobby updates
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = lobby });

                // Handling cases where player was unexpectedly removed from the lobby
                if (!IsPlayerInLobby()) {
                    Debug.Log("Lobby Warning: Not in Lobby Anymore!");
                    joinedLobby = null;
                }

                // Handling cases where player joined a lobby with a disconnected host
                if (!IsLobbyHost()) {
                    string hostCIDstring = GetPlayerFieldValueById(lobby.HostId, KEY_NETWORK_CLIENT_ID);

                    if (!ulong.TryParse(hostCIDstring, out ulong hostCID) || 
                        !NetworkManager.Singleton.ConnectedClients.ContainsKey(hostCID))
                    {
                        Debug.Log("Unusual activity from host in the network! Changing hosts...");
                        RemoveFromLobby(lobby.HostId);
                    }
                }
            }
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

    // Updates player activity status, and handles any necessary ownership transference
    public async void UpdatePlayerData(string field, string newValue) {

        joinedLobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, 
        new UpdatePlayerOptions {
            Data = new Dictionary<string, PlayerDataObject> {
                { field, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newValue) }
            }
        });

    }

    // Returns a player ID that matches a certain key-value serach
    public string GetPlayerIdByFieldValue(string field, string value) {
        foreach (var player in joinedLobby.Players)
        {
            if (player.Data != null && player.Data.ContainsKey(field)) {
                if (player.Data[field].Value == value)
                {
                    return player.Id;
                }
            }
        }

        return null;
    }

    // Returns the value of a field for a specific player by ID
    public string GetPlayerFieldValueById(string playerId, string field)
    {
        foreach (var player in joinedLobby.Players)
        {
            if (player.Id == playerId && player.Data != null && player.Data.ContainsKey(field))
            {
                return player.Data[field].Value;
            }
        }

        return null;
    }


    /*
     * --- UTILITY METHODS ---
     */

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
                message += "\n Is Host?: " + (joinedLobby.HostId == player.Id) + " Name: " + player.Data["PlayerName"].Value +  " Client: " + player.Data[KEY_NETWORK_CLIENT_ID].Value;;
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
                Debug.Log("Starting a new relay server...");

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

}