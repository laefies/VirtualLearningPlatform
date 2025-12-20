using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

/// <summary>
/// Manages multiplayer study lobbies (lobbies) for collaborative educational experiences.
/// Handles authentication, lobby creation/joining, heartbeats, and experience transitions.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    #region Singleton
    public static LobbyManager Instance { get; private set; }
    #endregion

    #region Constants
    // Lobby data keys
    private const string KEY_EXPERIENCE_NAME = "ExperienceName";
    private const string KEY_LOBBY_STATUS = "LobbyStatus";
    private const string KEY_RELAY_CODE = "RelayCode";

    // Player data keys
    private const string KEY_PLAYER_NAME = "PlayerName";

    // Lobby status values
    private const string STATUS_WAITING = "Waiting";
    private const string STATUS_IN_PROGRESS = "Learning";
    private const string STATUS_FULL = "Full";

    // Default values
    private const string NO_EXPERIENCE_SELECTED = "None";

    // Timing constants
    private const float HEARTBEAT_INTERVAL = 15f;
    private const float POLL_INTERVAL = 1.5f;

    // Lobby settings
    private const int DEFAULT_MAX_PLAYERS = 10;
    private const int LOBBY_QUERY_LIMIT = 30;
    #endregion

    #region Events
    public event Action<Lobby> OnLobbyJoined;
    public event Action OnLobbyLeft;
    public event Action<List<Lobby>> OnLobbyListRefreshed;
    public event Action<List<Player>> OnLobbyPlayersChanged;
    public event Action<string> OnExperienceChanged;
    public event Action OnRelayReady;
    #endregion

    #region Private Fields
    private Lobby _currentLobby;
    private string _cachedPlayerId;

    private float _heartbeatTimer;
    private float _pollTimer;

    private bool _isAuthenticated;
    private bool _isShuttingDown;
    private int _lobbyCounter;

    private int _lastKnownPlayerCount;
    private string _lastKnownHostId;
    private string _lastKnownExperience;
    private string _lastExperienceFilter;
    #endregion

    #region Properties
    public bool IsAuthenticated => _isAuthenticated;
    public bool IsInLobby => _currentLobby != null;
    public bool IsHost => IsInLobby && _currentLobby.HostId == _cachedPlayerId;
    public Lobby CurrentLobby => _currentLobby;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!_isAuthenticated) return;

        UpdateHeartbeat();
        UpdatePolling();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            CleanupOnExit();
            Instance = null;
        }
    }
    #endregion

    #region Authentication
    /// <summary>
    /// Authenticates the player with Unity Services. Should be called once, at app startup.
    /// </summary>
    public async Task<bool> AuthenticateAsync()
    {
        if (_isAuthenticated)
        {
            Debug.LogWarning("[Lobby Management] Already authenticated.");
            return true;
        }

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            _cachedPlayerId = AuthenticationService.Instance.PlayerId;
            _isAuthenticated = true;

            Debug.Log($"[Lobby Management] Successfully authenticated.");
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby Management] Authentication failed: {e.Message}");
            return false;
        }
    }
    #endregion

    #region Lobby Creation & Joining
    /// <summary>
    /// Creates a new lobby.
    /// </summary>
    /// <param name="playerName">Display name for the creating player</param>
    /// <param name="experienceName">Name of the experience/level to play (optional)</param>
    /// <param name="maxPlayers">Maximum number of players allowed</param>
    /// <param name="isPrivate">Whether the lobby is private</param>
    public async Task<Lobby> CreateLobbyAsync(
        string playerName,
        int maxPlayers = DEFAULT_MAX_PLAYERS,
        bool isPrivate = false)
    {
        if (!_isAuthenticated)
        {
            Debug.LogError("[Lobby Management] Cannot create lobby - not authenticated.");
            return null;
        }

        if (IsInLobby)
        {
            Debug.LogWarning("[Lobby Management] Already in a lobby. Leave current lobby first.");
            return _currentLobby;
        }

        try
        {
            _lobbyCounter++;
            string lobbyName = $"{playerName}'s";

            string experience = string.IsNullOrEmpty(_lastExperienceFilter) ? NO_EXPERIENCE_SELECTED : _lastExperienceFilter;

            string initialStatus = maxPlayers == 1 ? STATUS_FULL : STATUS_WAITING;

            var options = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = CreatePlayer(playerName),
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_EXPERIENCE_NAME, new DataObject(DataObject.VisibilityOptions.Public, experience, index: DataObject.IndexOptions.S1) },
                    { KEY_LOBBY_STATUS, new DataObject(DataObject.VisibilityOptions.Public, initialStatus) },
                    { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, "") }
                }
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            UpdateTrackedState();
            ResetTimers();

            string logMessage = experience == NO_EXPERIENCE_SELECTED
                ? $"[Lobby Management] Created '{lobbyName}' (no experience selected yet)"
                : $"[Lobby Management] Created '{lobbyName}' for experience '{experience}'";
            Debug.Log(logMessage);

            OnLobbyJoined?.Invoke(_currentLobby);

            return _currentLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Management] Failed to create lobby: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Joins an existing lobby.
    /// </summary>
    /// <param name="lobby">The lobby to join</param>
    /// <param name="playerName">Display name for the joining player (optional)</param>
    public async Task<bool> JoinLobbyAsync(Lobby lobby, string playerName)
    {
        if (!_isAuthenticated)
        {
            Debug.LogError("[Lobby Management] Cannot join lobby - not authenticated.");
            return false;
        }

        if (IsInLobby)
        {
            Debug.LogWarning("[Lobby Management] Already in a lobby. Leave current lobby first.");
            return false;
        }

        try
        {
            var options = new JoinLobbyByIdOptions
            {
                Player = CreatePlayer(playerName)
            };

            _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);

            await UpdateLobbyStatusBasedOnCapacity();

            UpdateTrackedState();
            ResetTimers();

            Debug.Log($"[Lobby Management] Joined '{lobby.Name}'");
            OnLobbyJoined?.Invoke(_currentLobby);
            OnLobbyPlayersChanged?.Invoke(_currentLobby.Players);
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Management] Failed to join lobby: {e.Message}");

            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                await RefreshLobbyListAsync();
            }

            return false;
        }
    }

    /// <summary>
    /// Leaves the current lobby.
    /// </summary>
    public async Task LeaveLobbyAsync()
    {
        if (!IsInLobby) return;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, _cachedPlayerId);

            Debug.Log($"[Lobby Management] Left '{_currentLobby.Name}'");

            _currentLobby = null;
            ClearTrackedState();
            OnLobbyLeft?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Management] Failed to leave lobby: {e.Message}");

            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                _currentLobby = null;
                ClearTrackedState();
                OnLobbyLeft?.Invoke();
            }
        }
    }
    #endregion

    #region Lobby Management
    /// <summary>
    /// Changes the selected experience. Only the host can do this.
    /// </summary>
    public async Task<bool> ChangeExperienceAsync(string newExperienceName)
    {
        if (!IsHost)
        {
            Debug.LogWarning("[Lobby Management] Only the host can change the experience.");
            return false;
        }

        try
        {
            var options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_EXPERIENCE_NAME, new DataObject(DataObject.VisibilityOptions.Public, newExperienceName, index: DataObject.IndexOptions.S1) }
                }
            };

            _currentLobby = await Lobbies.Instance.UpdateLobbyAsync(_currentLobby.Id, options);

            _lastKnownExperience = newExperienceName;

            Debug.Log($"[Lobby Management] Changed experience to '{newExperienceName}'");
            OnExperienceChanged?.Invoke(_lastKnownExperience);

            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Management] Failed to change experience: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Kicks a player from the lobby. Only the host can do this.
    /// </summary>
    public async Task<bool> KickPlayerAsync(string playerId)
    {
        if (!IsHost)
        {
            Debug.LogWarning("[Lobby Management] Only the host can kick players.");
            return false;
        }

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, playerId);

            _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);

            await UpdateLobbyStatusBasedOnCapacity();

            CheckAndFirePlayerChanges();

            Debug.Log($"[Lobby Management] Kicked player {playerId}");
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Management] Failed to kick player: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Transfers host privileges to another player.
    /// </summary>
    public async Task<bool> TransferHostAsync(string newHostId)
    {
        if (!IsHost)
        {
            Debug.LogWarning("[Lobby Management] Only the host can transfer host privileges.");
            return false;
        }

        try
        {
            var options = new UpdateLobbyOptions
            {
                HostId = newHostId
            };

            _currentLobby = await Lobbies.Instance.UpdateLobbyAsync(_currentLobby.Id, options);

            _lastKnownHostId = newHostId;

            Debug.Log($"[Lobby Management] Transferred host to {newHostId}");
            OnLobbyPlayersChanged?.Invoke(_currentLobby.Players);

            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Management] Failed to transfer host: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates the lobby status based on current capacity.
    /// Sets status to Full if at max capacity, otherwise Waiting (if not already in progress).
    /// </summary>
    private async Task UpdateLobbyStatusBasedOnCapacity()
    {
        if (!IsHost || !IsInLobby) return;

        // Don't change status if experience is already in progress
        string currentStatus = GetLobbyStatus(_currentLobby);
        if (currentStatus == STATUS_IN_PROGRESS) return;

        bool isFull = _currentLobby.Players.Count >= _currentLobby.MaxPlayers;
        string newStatus = isFull ? STATUS_FULL : STATUS_WAITING;

        if (currentStatus == newStatus) return;

        try
        {
            var options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_LOBBY_STATUS, new DataObject(DataObject.VisibilityOptions.Public, newStatus) }
                }
            };

            _currentLobby = await Lobbies.Instance.UpdateLobbyAsync(_currentLobby.Id, options);
            Debug.Log($"[Lobby Management] Updated lobby status to '{newStatus}'");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Management] Failed to update lobby status: {e.Message}");
        }
    }
    #endregion

    #region Lobby Discovery
    /// <summary>
    /// Refreshes the list of available lobbies.
    /// </summary>
    /// <param name="experienceFilter">Optional: filter by specific experience name</param>
    public async Task<List<Lobby>> RefreshLobbyListAsync(string experienceFilter = null, bool clearFilters = false)
    {
        if (!_isAuthenticated)
        {
            Debug.LogError("[Lobby Management] Cannot refresh - not authenticated.");
            return new List<Lobby>();
        }

        try
        {
            var options = new QueryLobbiesOptions
            {
                Count = LOBBY_QUERY_LIMIT,
                Order = new List<QueryOrder> {
                    new QueryOrder( asc: false, field: QueryOrder.FieldOptions.Created)
                }
            };

            if (clearFilters) ClearFilters();

            experienceFilter ??= _lastExperienceFilter;
            if (!string.IsNullOrEmpty(experienceFilter))
            {
                options.Filters ??= new List<QueryFilter>();
                options.Filters.Add(new QueryFilter(
                    field: QueryFilter.FieldOptions.S1,
                    op: QueryFilter.OpOptions.EQ,
                    value: experienceFilter));

                _lastExperienceFilter = experienceFilter;
            }

            var response = await Lobbies.Instance.QueryLobbiesAsync(options);

            Debug.Log($"[Lobby Management] Found {response.Results.Count} available lobbies. Filtered by: {experienceFilter ?? "All"}");
            OnLobbyListRefreshed?.Invoke(response.Results);

            return response.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Management] Failed to refresh lobby list: {e.Message}");
            return new List<Lobby>();
        }
    }
    #endregion

    #region Experience Start
    /// <summary>
    /// Prepares the multiplayer experience by creating a relay.
    /// Only the host can call this. After success, OnRelayReady event is fired.
    /// The caller is responsible for loading the scene after this succeeds.
    /// </summary>
    public async Task<bool> PrepareMultiplayerExperienceAsync()
    {
        if (!IsHost)
        {
            Debug.LogWarning("[Lobby Management] Only the host can prepare the experience.");
            return false;
        }

        if (!HasExperienceSelected())
        {
            Debug.LogWarning("[Lobby Management] Cannot prepare - no experience selected yet.");
            return false;
        }

        try
        {
            string relayCode = await RelayManager.Instance.CreateRelayAsync();

            var options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) },
                    { KEY_LOBBY_STATUS, new DataObject(DataObject.VisibilityOptions.Public, STATUS_IN_PROGRESS) }
                }
            };

            _currentLobby = await Lobbies.Instance.UpdateLobbyAsync(_currentLobby.Id, options);

            Debug.Log($"[Lobby Management] Relay ready for '{GetCurrentExperience()}'");
            
            OnRelayReady?.Invoke();
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby Management] Failed to prepare experience: {e.Message}");
            return false;
        }
    }
    #endregion

    #region Polling & Heartbeat
    private void UpdateHeartbeat()
    {
        if (!IsHost || !IsInLobby) return;

        _heartbeatTimer -= Time.deltaTime;

        if (_heartbeatTimer <= 0f)
        {
            _heartbeatTimer = HEARTBEAT_INTERVAL;
            SendHeartbeatAsync();
        }
    }

    private async void SendHeartbeatAsync()
    {
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby Management] Heartbeat failed: {e.Message}");
        }
    }

    private void UpdatePolling()
    {
        // Only poll if in a lobby and not yet in the experience scene
        if (!IsInLobby || RelayManager.Instance.IsInRelay()) return;

        _pollTimer -= Time.deltaTime;

        if (_pollTimer <= 0f)
        {
            _pollTimer = POLL_INTERVAL;
            PollLobbyStateAsync();
        }
    }

    private async void PollLobbyStateAsync()
    {
        try
        {
            var updatedLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
            _currentLobby = updatedLobby;

            // Check if player was removed from the lobby
            if (!IsPlayerInCurrentLobby())
            {
                Debug.Log("[Lobby Management] Removed from lobby");
                _currentLobby = null;
                ClearTrackedState();
                OnLobbyLeft?.Invoke();
                return;
            }

            // Check if host prepared the experience (relay code appears)
            string relayCode = GetRelayCode();
            if (!string.IsNullOrEmpty(relayCode) && !IsHost)
            {
                // Non-host players join the relay and fire event
                Debug.Log("[Lobby Management] Relay ready - joining as client");
                await RelayManager.Instance.JoinRelayAsync(relayCode);
                OnRelayReady?.Invoke();
                return;
            }

            // Check for changes and fire appropriate events
            CheckAndFirePlayerChanges();
            CheckAndFireExperienceChanges();
        }
        catch (LobbyServiceException e)
        {
            // Lobby no longer exists
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.Log("[Lobby Management] Lobby no longer exists");
                _currentLobby = null;
                ClearTrackedState();
                OnLobbyLeft?.Invoke();
            }
            else
            {
                Debug.LogError($"[Lobby Management] Polling failed: {e.Message}");
            }
        }
    }
    #endregion

    #region Change Detection
    /// <summary>
    /// Updates the tracked state after lobby changes.
    /// Call this whenever _currentLobby is updated.
    /// </summary>
    private void UpdateTrackedState()
    {
        if (!IsInLobby) return;

        _lastKnownPlayerCount = _currentLobby.Players?.Count ?? 0;
        _lastKnownHostId = _currentLobby.HostId;

        CheckAndFireExperienceChanges();
    }

    /// <summary>
    /// Clears the tracked state when leaving a lobby.
    /// </summary>
    private void ClearTrackedState()
    {
        _lastKnownPlayerCount = 0;
        _lastKnownHostId = null;

        ClearFilters();
        CheckAndFireExperienceChanges();
    }

    /// <summary>
    /// Clears all previously applied lobby list filters.
    /// </summary>
    private void ClearFilters()
    {
        _lastExperienceFilter = null;
    }


    /// <summary>
    /// Checks if players have changed.
    /// </summary>
    private void CheckAndFirePlayerChanges()
    {
        if (!IsInLobby) return;

        int currentPlayerCount = _currentLobby.Players?.Count ?? 0;
        string currentHostId = _currentLobby.HostId;

        bool playersChanged = currentPlayerCount != _lastKnownPlayerCount;
        bool hostChanged = currentHostId != _lastKnownHostId;

        if (playersChanged || hostChanged)
        {
            _lastKnownPlayerCount = currentPlayerCount;
            _lastKnownHostId = currentHostId;

            OnLobbyPlayersChanged?.Invoke(_currentLobby.Players);
        }
    }

    /// <summary>
    /// Checks if the experience has changed.
    /// </summary>
    private void CheckAndFireExperienceChanges()
    {
        string currentExperience = GetCurrentExperience();

        if (currentExperience != _lastKnownExperience)
        {
            _lastKnownExperience = currentExperience;

            Debug.Log($"[Lobby Management] Changed experience to '{_lastKnownExperience ?? "None"}'");
            OnExperienceChanged?.Invoke(_lastKnownExperience);
        }
    }
    #endregion

    #region Helper Methods
    private Player CreatePlayer(string playerName)
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
            }
        };
    }

    private bool IsPlayerInCurrentLobby()
    {
        if (!IsInLobby || _currentLobby.Players == null) return false;

        return _currentLobby.Players.Any(p => p.Id == _cachedPlayerId);
    }

    private void ResetTimers()
    {
        _heartbeatTimer = HEARTBEAT_INTERVAL;
        _pollTimer = POLL_INTERVAL;
    }

    private string GetRelayCode()
    {
        if (!IsInLobby || !_currentLobby.Data.ContainsKey(KEY_RELAY_CODE))
            return null;

        return _currentLobby.Data[KEY_RELAY_CODE].Value;
    }

    /// <summary>
    /// Gets the currently selected experience name from the lobby.
    /// Returns null if no experience has been selected yet.
    /// </summary>
    public string GetCurrentExperience()
    {
        if (!IsInLobby || !_currentLobby.Data.ContainsKey(KEY_EXPERIENCE_NAME))
            return null;

        string experience = _currentLobby.Data[KEY_EXPERIENCE_NAME].Value;
        return experience == NO_EXPERIENCE_SELECTED ? null : experience;
    }

    /// <summary>
    /// Checks if an experience has been selected for the current lobby.
    /// </summary>
    public bool HasExperienceSelected()
    {
        return GetCurrentExperience() != null;
    }

    /// <summary>
    /// Gets the current status of a given lobby.
    /// </summary>
    public string GetLobbyStatus(Lobby lobby)
    {
        if (lobby == null || !lobby.Data.ContainsKey(KEY_LOBBY_STATUS))
            return null;

        return lobby.Data[KEY_LOBBY_STATUS].Value;
    }


    /// <summary>
    /// Gets a player's display name from the current lobby.
    /// </summary>
    public string GetPlayerName(string playerId)
    {
        if (!IsInLobby) return null;

        var player = _currentLobby.Players.FirstOrDefault(p => p.Id == playerId);

        if (player?.Data != null && player.Data.ContainsKey(KEY_PLAYER_NAME))
        {
            return player.Data[KEY_PLAYER_NAME].Value;
        }

        return null;
    }

    /// <summary>
    /// Checks if the given player ID belongs to the host of the current lobby.
    /// </summary>
    public bool IsPlayerHost(string playerId)
    {
        if (!IsInLobby || string.IsNullOrEmpty(playerId))
            return false;

        return _currentLobby.HostId == playerId;
    }
    #endregion

    #region Application Lifecycle & Cleanup

    private void OnApplicationQuit()
    {
        CleanupOnExit();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) CleanupOnExit();
    }

    /// <summary>
    /// Performs cleanup when the application is closing.
    /// Removes player from lobby - Unity Lobbies handles host migration automatically.
    /// </summary>
    private void CleanupOnExit()
    {
        if (!IsInLobby || _isShuttingDown) return;

        _isShuttingDown = true;

        try
        {
            // Stop timers to prevent further updates
            _heartbeatTimer = float.MaxValue;
            _pollTimer = float.MaxValue;

            // The lobby service will auto-remove inactive players after ~30 seconds anyway
                    Debug.Log("OnApplicationPause " + _currentLobby.Id + " " + _cachedPlayerId);

            _ = LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, _cachedPlayerId);

            Debug.Log("[Lobby Management] Leaving lobby on application exit");

            _currentLobby = null;
            ClearTrackedState();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby Management] Error during cleanup: {e.Message}");
        }
    }

    #endregion
}