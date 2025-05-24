using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

// Responsible for identifying the current device and instantiating the corresponding player prefab. 
public class PlayerManager : NetworkBehaviour
{
    // List of all supported devices in the application:
    //   The first element (index 0) corresponds to the VR Simulator.
    [SerializeField] private List<DeviceInfo> supportedDevices = new List<DeviceInfo>();

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start() 
    {
        // Detect the current device or fallback to default
        DeviceInfo info = GetDeviceInfo();

        // Instantiate the device-specific player prefab as a child of this manager
        GameObject deviceInstance = Instantiate(info.playerPrefab, transform);

        // Initialize the global device state
        DeviceManager.Instance.Initialize(info);
    }

    // Returns the DeviceInfo that corresponds to the current device model
    private DeviceInfo GetDeviceInfo()
    {
        // Goes through supported devices and compares 
        // it to the system info provided by the device;
        foreach (DeviceInfo info in supportedDevices) {
            if (SystemInfo.deviceModel.Contains(info.deviceName))
                return info;
        }

        // Should no match is found, the first entry (Simulator) is returned;
        return supportedDevices[0];
    }
}