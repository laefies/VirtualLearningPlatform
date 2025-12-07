using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

// Responsible for identifying the current device and instantiating the corresponding player prefab. 
public class PlayerManager : NetworkBehaviour
{
    #region Private Fields
    private string _playerName;
    #endregion

    #region Properties
    public string PlayerName => _playerName;
    #endregion

    public static PlayerManager Instance { get; private set; }

    [SerializeField] private List<DeviceInfo> supportedDevices = new List<DeviceInfo>();

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

    private void Start() 
    {
        // Authenticate into Unity Services
        LobbyManager.Instance.AuthenticateAsync();

        // Detect the current device or fallback to default
        DeviceInfo info = GetDeviceInfo();
        _playerName = $"{info.deviceName} User";

        // Instantiate the device-specific player prefab as a child of this manager
        GameObject deviceInstance = Instantiate(info.playerPrefab, transform);

        // Initialize the global device state
        DeviceManager.Instance.Initialize(info);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    #endregion


    // Returns the DeviceInfo that corresponds to the current device model
    private DeviceInfo GetDeviceInfo()
    {
        // Goes through supported devices and compares 
        // it to the system info provided by the device;
        foreach (DeviceInfo info in supportedDevices)
        {
            if (SystemInfo.deviceModel.Contains(info.deviceName))
                return info;
        }

        // Should no match is found, the first entry (Simulator) is returned;
        return supportedDevices[0];
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.SceneManager.OnSceneEvent += OnNetworkSceneEvent;
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.SceneManager.OnSceneEvent -= OnNetworkSceneEvent;
        base.OnNetworkDespawn();
    }

    private void OnNetworkSceneEvent(SceneEvent sceneEvent) {

        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete) {
            // TODO DELETE SceneLoader.Instance.NotifySceneLoad();
        }
    }

}