using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Netcode;

/// <summary>
/// Represents an object shared across all users.
/// VR users share world positions directly.
/// AR users use anchor-relative positioning.
/// </summary>
public class SharedObject : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject vrProxyObject;
    [SerializeField] private Transform anchorPoint;

    private ObjectTypeId _typeId;
    private bool _isDetectedByLocalUser = false;
    private Transform[] _allChildTransforms;

    private NetworkVariable<NetworkPose> _sharedPose = new NetworkVariable<NetworkPose>(
        new NetworkPose(Vector3.zero, Quaternion.identity),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool IsARUser() => DeviceManager.Instance.IsAR();
    public GameObject GetVRProxy()     => vrProxyObject;
    public Transform  GetAnchorPoint() => anchorPoint;

    private void Awake()
    {
        CacheChildTransforms();
        
        vrProxyObject?.SetActive(!IsARUser());
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // For non-AR users:
        if (!IsARUser()) {
            // Listen to changes to the shared object pose
            _sharedPose.OnValueChanged += OnSharedPoseChanged;
            ApplySharedPose(); // And apply the starting one

            // Immediately set as spotted - VR users always see the shared objects
            _isDetectedByLocalUser = true;
        }
        
        UpdateVisibility(_isDetectedByLocalUser);
    }

    public void Initialize(ObjectTypeId typeId) { _typeId = typeId; }

    /// <summary>
    /// Called on the specific client that detected/placed this object
    /// </summary>
    [ClientRpc]
    public void NotifyClientDetectionClientRpc(NetworkPose pose, ClientRpcParams clientRpcParams = default)
    {
        if (anchorPoint == null) return; 
            
        _isDetectedByLocalUser = true;
        
        if (IsARUser()) {
            // AR: Position the anchor "on" the real-world detected marker
            anchorPoint.SetPositionAndRotation(pose.position, pose.rotation);
        } else {
            // VR: Update the shared value of the object's pose
            UpdateSharedPoseServerRpc(pose);
        }
        
        UpdateVisibility(true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateSharedPoseServerRpc(NetworkPose pose) {
        _sharedPose.Value = pose;
    }

    private void OnSharedPoseChanged(NetworkPose oldValue, NetworkPose newValue) {
        if (!IsARUser()) ApplySharedPose();
    }

    private void ApplySharedPose() {
        Pose newSharedPose = _sharedPose.Value.ToPose();
        transform.SetPositionAndRotation(newSharedPose.position, newSharedPose.rotation);
    }

    private void UpdateVisibility(bool visible)
    {
        if (_allChildTransforms == null)
            CacheChildTransforms();

        int targetLayer = LayerMask.NameToLayer(visible ? "Default" : "Hidden");

        foreach (Transform t in _allChildTransforms)
        {
            if (t == null) continue;
            
            GameObject obj = t.gameObject;

            if (vrProxyObject != null && obj == vrProxyObject) continue;

            obj.layer = targetLayer;
            SetComponentsEnabled(obj, visible);
        }
    }

    private void SetComponentsEnabled(GameObject obj, bool enabled)
    {
        foreach (Graphic g in obj.GetComponents<Graphic>())
            g.enabled = enabled;

        foreach (Collider c in obj.GetComponents<Collider>())
            c.enabled = enabled;

        foreach (XRBaseInteractable i in obj.GetComponents<XRBaseInteractable>())
            i.enabled = enabled;
    }

    private void CacheChildTransforms() {
        _allChildTransforms = GetComponentsInChildren<Transform>(includeInactive: true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestOwnershipServerRpc(ServerRpcParams rpcParams = default) {
        NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    public void ReturnOwnershipServerRpc() {
        NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsARUser()) _sharedPose.OnValueChanged -= OnSharedPoseChanged;

        SharedObjectRegistry.Instance?.UnregisterObject(_typeId);
        
        base.OnNetworkDespawn();
    }

    void OnValidate() { UpdateVisibility(false); }
}