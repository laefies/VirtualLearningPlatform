using UnityEngine;

// Responsible for storing information regarding the device, and handle its subsystems.
public class DeviceManager : MonoBehaviour
{
    // Singleton instance for global access
    public static DeviceManager Instance { get; private set; }

    // Information regarding the active device.
    private DeviceInfo _currentDevice;

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

    // Initializes the DeviceManager with the specified DeviceInfo.
    // Called during startup.
    public void Initialize(DeviceInfo info)
    {
        _currentDevice = info;
        Debug.Log("[Device Manager] Starting! Device recognized as '" + info.deviceName + "'.");
    }

    // Returns true if the current device is categorized as AR.
    public bool IsAR() => _currentDevice?.deviceType == DeviceType.AR;

    // Returns true if the current device is categorized as VR.
    public bool IsVR() => _currentDevice?.deviceType == DeviceType.VR;
}