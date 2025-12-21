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
        SharedObject sharedObj = GetOrSpawnObject(info);
        if (sharedObj == null) return;

        // Notify the specific client that placed/detected it
        sharedObj.NotifyClientDetectionClientRpc(info.localPose,
            new ClientRpcParams {
                Send = new ClientRpcSendParams
                { TargetClientIds = new ulong[] { senderId } }
            });
    }

    private SharedObject GetOrSpawnObject(ObjectPlacementInfo info)
    {
        // 1. Obtain the type of object that was placed/detected
        ObjectTypeId typeId = info.typeId;
        
        // 2. Check if this type's associated object is already in-scene
        if (_activeObjects.TryGetValue(typeId.value, out SharedObject existing))
            return existing;

        // 3. If not in-scene yet, fetch the associated object from the database
        GameObject prefab = prefabDatabase.GetPrefab(typeId);
        if (prefab == null) {
            Debug.LogWarning($"No prefab found for object type: {typeId}");
            return null;
        }

        // 4. Place the object at the default spawn position, initially
        GameObject instance = Instantiate(prefab, transform.position, transform.rotation);
        instance.transform.localScale *= info.detectedSize;

        // 5. Spawn the object for all users
        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        if (networkObject == null) {
            Debug.LogError($"Prefab for {typeId} is missing NetworkObject component!");
            Destroy(instance);
            return null;
        }
    
        networkObject.Spawn(true);

        // 6. Ensure the right type of object was instantiated, and save it
        SharedObject sharedObject = instance.GetComponent<SharedObject>();
        if (sharedObject != null) {
            sharedObject.Initialize(typeId);
            _activeObjects[typeId.value] = sharedObject;
        }

        return sharedObject;
    }

    public SharedObject GetObject(ObjectTypeId typeId) {
        _activeObjects.TryGetValue(typeId.value, out SharedObject sharedObject);
        return sharedObject;
    }

    public void UnregisterObject(ObjectTypeId typeId) { _activeObjects.Remove(typeId.value); }
}