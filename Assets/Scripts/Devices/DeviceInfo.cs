using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DeviceType { AR, VR, Other }

[CreateAssetMenu(menuName = "Device Info")]
public class DeviceInfo : ScriptableObject
{
    // Name of the device - should match the information given by the device system
    public string deviceName;

    // Could be AR, VR or Other, with the latter being used by the Simulator
    public DeviceType deviceType;

    // Prefab asset with a DeviceManager component that handles this specific device
    public GameObject playerPrefab;
}