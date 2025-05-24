using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DeviceType { AR, VR, Other }

[CreateAssetMenu(menuName = "Device Info")]
public class DeviceInfo : ScriptableObject
{
    public string deviceName;
    public DeviceType deviceType;
    public GameObject playerPrefab;
}