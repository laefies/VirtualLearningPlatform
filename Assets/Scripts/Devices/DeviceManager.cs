using UnityEngine;

// Responsible for storing information regarding the device, and handle its subsystems.
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

    void Start() {
        SceneLoader.Instance.OnSceneLoaded += PrepareDeviceForScene;
    }

    void OnDestroy() {
        SceneLoader.Instance.OnSceneLoaded -= PrepareDeviceForScene;
    }

    // Initializes the DeviceManager with the specified DeviceInfo.
    // Called during startup.
    public void Initialize(DeviceInfo info)
    {
        _deviceInfo = info;
        Debug.Log("[Device Manager] Starting! Device recognized as '" + info.deviceName + "'.");
    }

    // Returns true if the current device is categorized as AR.
    public bool IsAR() => _deviceInfo?.deviceType == DeviceType.AR;

    // Returns true if the current device is categorized as VR.
    public bool IsVR() => _deviceInfo?.deviceType == DeviceType.VR;

    void PrepareDeviceForScene(object sender, SceneLoader.SceneEventArgs e) {
        Debug.Log("[Device Manager] Preparing device for new scene => '" + e.sceneInfo.displayName + "'.");

        if (!IsAR() && e.sceneInfo.vrEnvironmentPrefab != null) {
            Instantiate(e.sceneInfo.vrEnvironmentPrefab);
        }
    }

}