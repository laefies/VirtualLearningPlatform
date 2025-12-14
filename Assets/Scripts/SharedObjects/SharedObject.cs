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

    private NetworkVariable<NetworkPose> _worldTransform = new NetworkVariable<NetworkPose>(
        new NetworkPose(Vector3.zero, Quaternion.identity),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> _scale = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        CacheChildTransforms();
        
        if (vrProxyObject != null)
            vrProxyObject.SetActive(!IsARUser());
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        _worldTransform.OnValueChanged += OnWorldTransformChanged;
        _scale.OnValueChanged += OnScaleChanged;
        
        // VR users can see immediately and use world position
        if (!IsARUser())
        {
            _isDetectedByLocalUser = true;
            ApplyWorldTransform();
        }
        
        UpdateVisibility(_isDetectedByLocalUser);
    }

    public void Initialize(ObjectTypeId typeId)
    {
        _typeId = typeId;
    }

    /// <summary>
    /// Called on the specific client that detected/placed this object
    /// </summary>
    [ClientRpc]
    public void NotifyClientDetectionClientRpc(NetworkPose localPose, float size, ClientRpcParams clientRpcParams = default)
    {
        _isDetectedByLocalUser = true;
        
        if (IsARUser())
        {
            // AR: Position anchor at detected marker
            if (anchorPoint != null)
            {
                anchorPoint.SetPositionAndRotation(localPose.position, localPose.rotation);
                anchorPoint.localScale = Vector3.one * size;
            }
        }
        else
        {
            UpdateWorldTransformServerRpc(localPose, size);
        }
        
        UpdateVisibility(true);
    }

    private void OnWorldTransformChanged(NetworkPose oldValue, NetworkPose newValue)
    {
        if (!IsARUser())
            ApplyWorldTransform();
    }

    private void OnScaleChanged(float oldValue, float newValue)
    {
        if (!IsARUser())
            ApplyWorldTransform();
    }

    private void ApplyWorldTransform()
    {
        // VR users apply the shared world transform directly
        Pose worldPose = _worldTransform.Value.ToPose();
        transform.SetPositionAndRotation(worldPose.position, worldPose.rotation);
        transform.localScale = Vector3.one * _scale.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateWorldTransformServerRpc(NetworkPose pose, float scale)
    {
        _worldTransform.Value = pose;
        _scale.Value          = scale;
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

            if (vrProxyObject != null && obj == vrProxyObject)
                continue;

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

    private void CacheChildTransforms()
    {
        _allChildTransforms = GetComponentsInChildren<Transform>(includeInactive: true);
    }

    private bool IsARUser()
    {
        return DeviceManager.Instance != null && DeviceManager.Instance.IsAR();
    }

    public Transform GetAnchorPoint() => anchorPoint;

    [ServerRpc(RequireOwnership = false)]
    public void RequestOwnershipServerRpc(ServerRpcParams rpcParams = default)
    {
        NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    public void ReturnOwnershipServerRpc()
    {
        NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
    }

    public override void OnNetworkDespawn()
    {
        _worldTransform.OnValueChanged -= OnWorldTransformChanged;
        _scale.OnValueChanged -= OnScaleChanged;
        
        if (SharedObjectRegistry.Instance != null)
            SharedObjectRegistry.Instance.UnregisterObject(_typeId);
        
        base.OnNetworkDespawn();
    }
}