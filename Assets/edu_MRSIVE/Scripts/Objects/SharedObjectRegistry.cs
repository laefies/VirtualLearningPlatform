using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-authoritative registry that manages all shared objects in the scene.
/// Handles spawning, tracking, and anchor relationships.
/// </summary>
public class SharedObjectRegistry : NetworkBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ObjectPrefabDatabase prefabDatabase;
    [SerializeField] private Transform defaultVRSpawnPoint;

    private Dictionary<string, SharedObject> _activeObjects = new Dictionary<string, SharedObject>();

    public static SharedObjectRegistry Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple SharedObjectRegistry instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public ObjectPrefabDatabase Database => prefabDatabase;

    /// <summary>
    /// Called when AR detects a marker or VR manually places an object
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RegisterObjectPlacementServerRpc(ObjectPlacementInfo info, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        
        // Get or create the shared object
        SharedObject sharedObj = GetOrSpawnObject(info.typeId);
        if (sharedObj == null) return;

        // Notify the specific client that placed/detected it
        sharedObj.NotifyClientDetectionClientRpc(info.localPose, info.detectedSize,
            new ClientRpcParams {
                Send = new ClientRpcSendParams
                { TargetClientIds = new ulong[] { senderId } }
            });
    }

    private SharedObject GetOrSpawnObject(ObjectTypeId typeId)
    {
        // Check if the object type has already been spawned
        if (_activeObjects.TryGetValue(typeId.value, out SharedObject existing))
            return existing;

        // Otherwise, get associated prefab from database
        GameObject prefab = prefabDatabase.GetPrefab(typeId);
        if (prefab == null) {
            Debug.LogWarning($"No prefab found for object type: {typeId}");
            return null;
        }

        // Initially place at default spawn position, for VR users
        Vector3 spawnPos    = defaultVRSpawnPoint != null ? defaultVRSpawnPoint.position : Vector3.zero;
        Quaternion spawnRot = defaultVRSpawnPoint != null ? defaultVRSpawnPoint.rotation : Quaternion.identity;

        GameObject instance = Instantiate(prefab, spawnPos, spawnRot);
        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        
        if (networkObject == null) {
            Debug.LogError($"Prefab for {typeId} is missing NetworkObject component!");
            Destroy(instance);
            return null;
        }

        networkObject.Spawn(true);

        SharedObject sharedObject = instance.GetComponent<SharedObject>();
        if (sharedObject != null) {
            sharedObject.Initialize(typeId);
            _activeObjects[typeId.value] = sharedObject;
        }

        return sharedObject;
    }

    public SharedObject GetObject(ObjectTypeId typeId)
    {
        _activeObjects.TryGetValue(typeId.value, out SharedObject sharedObject);
        return sharedObject;
    }

    public void UnregisterObject(ObjectTypeId typeId)
    {
        _activeObjects.Remove(typeId.value);
    }
}