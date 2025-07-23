using UnityEngine;
using Unity.Netcode;

// Responsible for storing information regarding the device, and handle its subsystems.
[RequireComponent(typeof(NetworkObject))]
public class DeviceManager : MonoBehaviour
{
    // Singleton instance for global access
    public static DeviceManager Instance { get; private set; }

    // Information regarding the active device.
    private DeviceInfo _deviceInfo;

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

    // Subscribe / Unsubscribe from scene events
    void Start() { SceneLoader.Instance.OnSceneLoaded += PrepareDeviceForScene; }
    void OnDestroy() { SceneLoader.Instance.OnSceneLoaded -= PrepareDeviceForScene; }

    // Called during startup - assigns the manager to a specified DeviceInfo.
    public void Initialize(DeviceInfo info)
    {
        _deviceInfo = info;
        Debug.Log("[Device Manager] Starting! Device recognized as '" + info.deviceName + "'.");
    }

    // Returns true if the current device is categorized as AR;
    public bool IsAR() => _deviceInfo?.deviceType == DeviceType.AR;

    void PrepareDeviceForScene(object sender, SceneLoader.SceneEventArgs e)
    {
        Debug.Log("[Device Manager] Preparing requirements for new scene...");

        // If the current device is not AR (meaning it is either VR or Simulator),
        // a backdrop should be shown to provide better user immersion / visual environment.
        if (!IsAR() && e.sceneInfo.vrEnvironmentPrefab != null)
            Instantiate(e.sceneInfo.vrEnvironmentPrefab);
    }

}