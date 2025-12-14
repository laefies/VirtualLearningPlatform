using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Manages Unity Relay connections for networked multiplayer games.
/// Handles relay allocation creation (host) and joining (client).
/// </summary>
public class RelayManager : MonoBehaviour
{
    #region Constants
    
    private const string RELAY_CONNECTION_TYPE = "dtls";
    private const int DEFAULT_MAX_CONNECTIONS  = 10;
    
    #endregion

    #region Singleton
    
    public static RelayManager Instance { get; private set; }
    
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Creates a relay allocation and starts the host.
    /// </summary>
    /// <param name="maxConnections">Maximum number of client connections (excluding host)</param>
    /// <returns>Join code for clients to connect, or null if failed</returns>
    public async Task<string> CreateRelayAsync(int maxConnections = DEFAULT_MAX_CONNECTIONS)
    {
        if (!ValidateNetworkManager()) return null;

        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var relayServerData = new RelayServerData(allocation, RELAY_CONNECTION_TYPE);
            GetUnityTransport().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();

            Debug.Log($"[Relay Management] Relay created successfully. Join Code: {joinCode}");
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay Management] Failed to create relay: {e}");
            return null;
        }
    }

    /// <summary>
    /// Joins an existing relay using a join code and starts the client.
    /// </summary>
    /// <param name="joinCode">The join code provided by the host</param>
    /// <returns>True if successfully joined, false otherwise</returns>
    public async Task<bool> JoinRelayAsync(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("[Relay Management] Join code cannot be null or empty.");
            return false;
        }

        if (!ValidateNetworkManager()) return false;

        try
        {
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var relayServerData = new RelayServerData(joinAllocation, RELAY_CONNECTION_TYPE);
            GetUnityTransport().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();

            Debug.Log($"[Relay Management] Successfully joined relay.");
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay Management] Failed to join relay: {e}");
            return false;
        }
    }

    /// <summary>
    /// Checks if currently connected to a relay (either as host or client).
    /// </summary>
    public bool IsInRelay()
    {
        var networkManager = NetworkManager.Singleton;
        
        if (networkManager == null || !networkManager.IsListening)
        {
            return false;
        }

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null) return false;

        return networkManager.IsHost || networkManager.IsClient;
    }

    /// <summary>
    /// Disconnects from the current relay session.
    /// </summary>
    public void DisconnectFromRelay()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[RelayManager] Not connected to any relay.");
            return;
        }

        NetworkManager.Singleton.Shutdown();
        Debug.Log("[Relay Management] Disconnected from relay.");
    }
    
    #endregion

    #region Private Helper Methods
    
    private bool ValidateNetworkManager()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[RelayManager] NetworkManager.Singleton not found in scene.");
            return false;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[Relay Management] UnityTransport component not found on NetworkManager.");
            return false;
        }

        return true;
    }

    private UnityTransport GetUnityTransport()
    {
        return NetworkManager.Singleton.GetComponent<UnityTransport>();
    }
    
    #endregion
}