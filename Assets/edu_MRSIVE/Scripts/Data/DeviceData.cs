using UnityEngine;

public enum DeviceType { AR, VR, Other }

/// <summary>
/// ScriptableObject that defines device-specific configuration.
/// </summary>
[CreateAssetMenu(fileName = "New Device Data", menuName = "edu_MRSIVE/Data/Device Data")]
public class DeviceData : ScriptableObject
{
    [Header("Device Identification")]
    [Tooltip("Device name to match against SystemInfo.deviceModel")]
    public string deviceName;

    [Tooltip("Type of XR device")]
    public DeviceType deviceType;

    [Header("Player Configuration")]
    [Tooltip("Prefab with LocalPlayer component to spawn for this device")]
    public GameObject playerPrefab;

    void OnValidate()
    {
        // Validate that the prefab has the required component
        if (playerPrefab != null && playerPrefab.GetComponent<LocalPlayer>() == null) {
            Debug.LogWarning($"[DeviceData] '{name}': Player prefab is missing LocalPlayer component!");
        }
    }
}