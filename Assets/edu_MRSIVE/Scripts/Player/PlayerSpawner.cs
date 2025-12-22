using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles player spawning based on the detected device.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    #region Singleton
    public static PlayerSpawner Instance { get; private set; }
    #endregion

    #region Configuration
    [SerializeField] private List<DeviceData> supportedDevices = new List<DeviceData>();
    #endregion

    #region Private Fields
    private DeviceData detectedDevice;
    private GameObject spawnedPlayer;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    #endregion

    #region Player Spawning
    /// <summary>
    /// Spawns the player prefab matching the current device.
    /// </summary>
    public void SpawnLocalPlayer()
    {
        if (spawnedPlayer != null)
        {
            Debug.LogWarning("[PlayerSpawner] Player has already been already spawned!");
            return;
        }

        detectedDevice = DetectDevice();
        string playerName = $"{detectedDevice.deviceName} User";

        // Instantiate the device-specific player rig prefab
        spawnedPlayer = Instantiate(detectedDevice.playerPrefab);

        // Initialize the local player
        LocalPlayer localPlayer = spawnedPlayer.GetComponent<LocalPlayer>();
        if (localPlayer != null) {
            localPlayer.Initialize(detectedDevice, playerName);
        } else {
            Debug.LogError("[PlayerSpawner] Player prefab is missing LocalPlayer component!");
        }
    }

    /// <summary>
    /// Network-spawn the existing local player when joining multiplayer.
    /// Should be called when connecting to a multiplayer session.
    /// </summary>
    public void NetworkSpawnPlayer()
    {
        if (spawnedPlayer == null) {
            Debug.LogError("[PlayerSpawner] Cannot network spawn - local player does not exist!");
            return;
        }

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer) {
            Debug.LogError("[PlayerSpawner] Cannot network spawn - not connected to a network!");
            return;
        }

        NetworkObject networkObject = spawnedPlayer.GetComponent<NetworkObject>();
        if (networkObject != null) {
            if (!networkObject.IsSpawned) {
                networkObject.SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
                Debug.Log("[PlayerSpawner] Player has been network spawned.");
            } else {
                Debug.LogWarning("[PlayerSpawner] Player had already been network spawned!");
            }
        } else {
            Debug.LogError("[PlayerSpawner] Player prefab missing NetworkObject component!");
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Detects the current device and returns matching DeviceData.
    /// </summary>
    private DeviceData DetectDevice()
    {
        string deviceModel = SystemInfo.deviceModel;

        foreach (DeviceData data in supportedDevices)  {
            if (deviceModel.Contains(data.deviceName)) {
                Debug.Log($"[PlayerSpawner] Detected device: {data.deviceName}");
                return data;
            }
        }

        if (supportedDevices.Count > 0) {
            Debug.LogWarning($"[PlayerSpawner] No matching device for '{deviceModel}'. Using default: {supportedDevices[0].deviceName}");
            return supportedDevices[0];
        }

        Debug.LogError("[PlayerSpawner] No devices configured!");
        return null;
    }

    /// <summary> Gets the currently detected device data (before spawning). </summary>
    public DeviceData GetDetectedDevice() {        
        return detectedDevice ?? DetectDevice();
    }
    #endregion
}