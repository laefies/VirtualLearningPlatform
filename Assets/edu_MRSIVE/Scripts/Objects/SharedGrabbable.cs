using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Netcode;

/// <summary>
/// A child object that can be grabbed and moved, but maintains its relationship
/// to the parent SharedObject's anchor. Example: A Sun that orbits above a solar panel.
/// </summary>
public class SharedGrabbable : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private SharedObject parentSharedObject;
    [SerializeField] private XRGrabInteractable grabInteractable;
    
    [Header("Docking Configuration")]
    [SerializeField] private bool startDocked = true;
    [SerializeField] private float autoUndockDistance = 2f;
    [SerializeField] private bool enableAutoUndock = true;

    public event Action<bool> OnDockedChanged;

    private NetworkVariable<Vector3> _relativePosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<Quaternion> _relativeRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> _isDocked = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Transform _anchorTransform;
    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;
    private bool _isBeingGrabbed = false;

    private void Awake()
    {
        ValidateComponents();
        
        _initialLocalPosition = transform.localPosition;
        _initialLocalRotation = transform.localRotation;
    }

    private void ValidateComponents()
    {
        if (parentSharedObject == null)
        {
            parentSharedObject = GetComponentInParent<SharedObject>();
            if (parentSharedObject == null)
                Debug.LogError($"[{gameObject.name}] SharedDockableObject requires a SharedObject parent!", this);
        }

        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
                Debug.LogError($"[{gameObject.name}] No XRGrabInteractable found!", this);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _anchorTransform = transform.parent;

        if (IsServer)
        {
            _isDocked.Value = startDocked;
            _relativePosition.Value = _initialLocalPosition;
            _relativeRotation.Value = _initialLocalRotation;
        }

        _relativePosition.OnValueChanged += OnRelativePositionChanged;
        _relativeRotation.OnValueChanged += OnRelativeRotationChanged;
        _isDocked.OnValueChanged += OnDockedStateChanged;

        if (grabInteractable != null) {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        ApplyRelativeTransform();
        UpdateDockingState(_isDocked.Value);
    }

    private void Update()
    {
        if (IsOwner && (_isBeingGrabbed || !_isDocked.Value))
            UpdateRelativeTransform();

        if (enableAutoUndock && _isDocked.Value && _isBeingGrabbed)
            CheckAutoUndock();
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        _isBeingGrabbed = true;

        if (parentSharedObject != null)
        {
            NetworkObject parentNetObj = parentSharedObject.GetComponent<NetworkObject>();
            if (parentNetObj != null && !parentNetObj.IsOwner)
            {
                parentSharedObject.RequestOwnershipServerRpc();
            }
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        _isBeingGrabbed = false;

        if (parentSharedObject != null && !IsServer)
        {
            parentSharedObject.ReturnOwnershipServerRpc();
        }
    }

    private void CheckAutoUndock()
    {
        if (_anchorTransform == null) return;

        float distance = Vector3.Distance(transform.position, _anchorTransform.position);
        
        if (distance > autoUndockDistance && _isDocked.Value)
            SetDockedServerRpc(false);
    }

    private void UpdateRelativeTransform()
    {
        if (_anchorTransform == null) return;

        // Calculate position and rotation relative to anchor
        Vector3 newRelativePos = _anchorTransform.InverseTransformPoint(transform.position);
        Quaternion newRelativeRot = Quaternion.Inverse(_anchorTransform.rotation) * transform.rotation;

        // Only update if changed significantly (reduce network traffic)
        if (Vector3.Distance(newRelativePos, _relativePosition.Value) > 0.001f ||
            Quaternion.Angle(newRelativeRot, _relativeRotation.Value) > 0.1f)
        {
            UpdateRelativeTransformServerRpc(newRelativePos, newRelativeRot);
        }
    }

    private void ApplyRelativeTransform()
    {
        if (_anchorTransform == null || _isBeingGrabbed) return;

        // Convert relative transform to world space
        Vector3 worldPos = _anchorTransform.TransformPoint(_relativePosition.Value);
        Quaternion worldRot = _anchorTransform.rotation * _relativeRotation.Value;

        transform.SetPositionAndRotation(worldPos, worldRot);
    }

    private void UpdateDockingState(bool isDocked)
    {
        if (isDocked) {
            transform.SetParent(_anchorTransform);
            transform.localPosition = _initialLocalPosition;
            transform.localRotation = _initialLocalRotation;
        }
        else {
            transform.SetParent(_anchorTransform);
        }
    }

    // Network variable callbacks
    private void OnRelativePositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        if (!IsOwner)
            ApplyRelativeTransform();
    }

    private void OnRelativeRotationChanged(Quaternion oldValue, Quaternion newValue)
    {
        if (!IsOwner)
            ApplyRelativeTransform();
    }

    private void OnDockedStateChanged(bool oldValue, bool newValue)
    {
        UpdateDockingState(newValue);
        OnDockedChanged?.Invoke(newValue);
    }

    // Server RPCs
    [ServerRpc]
    private void UpdateRelativeTransformServerRpc(Vector3 position, Quaternion rotation)
    {
        _relativePosition.Value = position;
        _relativeRotation.Value = rotation;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetDockedServerRpc(bool docked)
    {
        _isDocked.Value = docked;
    }

    public void SetDocked(bool docked)
    {
        SetDockedServerRpc(docked);
    }

    public void ResetToDockedPosition()
    {
        if (IsServer) {
            _relativePosition.Value = _initialLocalPosition;
            _relativeRotation.Value = _initialLocalRotation;
            _isDocked.Value = true;
        }
        else {
            ResetToDockedPositionServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetToDockedPositionServerRpc()
    {
        _relativePosition.Value = _initialLocalPosition;
        _relativeRotation.Value = _initialLocalRotation;
        _isDocked.Value = true;
    }

    public override void OnNetworkDespawn()
    {
        _relativePosition.OnValueChanged -= OnRelativePositionChanged;
        _relativeRotation.OnValueChanged -= OnRelativeRotationChanged;
        _isDocked.OnValueChanged -= OnDockedStateChanged;

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        base.OnNetworkDespawn();
    }
}