using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : NetworkBehaviour
{
    [System.Serializable]
    public class DeviceMapping
    {
        public string deviceIdentifier;
        public GameObject devicePrefab;
    }

    [SerializeField] private GameObject defaultDevicePrefab;
    [SerializeField] private List<DeviceMapping> deviceMappings = new List<DeviceMapping>();

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start() 
    {
        // Get both prefab and device identifier
        (GameObject devicePrefab, string deviceName) = GetPlayerPrefabAndDeviceName();
        Debug.Log("Starting! Device recognized as '" + deviceName + "'.");

        // Instantiate the device prefab
        GameObject deviceInstance = Instantiate(devicePrefab, transform);
    }

    private (GameObject, string) GetPlayerPrefabAndDeviceName()
    {
        foreach (var mapping in deviceMappings)
        {
            if (SystemInfo.deviceModel.Contains(mapping.deviceIdentifier))
                return (mapping.devicePrefab, mapping.deviceIdentifier);
        }

        return (defaultDevicePrefab, "Test Device");
    }


    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log("Spawning onto network! => Client " + GetComponent<NetworkObject>().OwnerClientId);
 
            // Rename player object with the device identifier
            // LobbyManager.Instance.UpdatePlayerData(
            //     LobbyManager.KEY_NETWORK_CLIENT_ID, 
            //     GetComponent<NetworkObject>().OwnerClientId.ToString()
            // );
        }
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("Despawning from network! => Client " + GetComponent<NetworkObject>().OwnerClientId);

        if (IsHost) {
            string playerID = LobbyManager.Instance.GetPlayerIdByFieldValue(
                LobbyManager.KEY_NETWORK_CLIENT_ID, 
                GetComponent<NetworkObject>().OwnerClientId.ToString()
            );
            if (playerID != null) LobbyManager.Instance.RemoveFromLobby(playerID);
        }

    }


}