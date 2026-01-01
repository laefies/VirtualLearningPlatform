using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Netcode;

/// <summary>
/// A child object that can be grabbed and moved, but maintains its relationship
/// to a parenting anchor.
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

    private NetworkVariable<NetworkPose> _relativePose = new NetworkVariable<NetworkPose>(
        new NetworkPose(Vector3.zero, Quaternion.identity),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> _isDocked = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Transform _anchor;
    private bool _isBeingGrabbed = false;

    private void Awake() { ValidateComponents(); }

    private void ValidateComponents()
    {
        if (parentSharedObject == null) {
            parentSharedObject = transform.root.GetComponent<SharedObject>();
            if (parentSharedObject == null)
                Debug.LogError($"[{gameObject.name}] SharedGrabbable requires a SharedObject parent!", this);
        }

        if (grabInteractable == null) {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
                Debug.LogError($"[{gameObject.name}] SharedGrabbable requires a XRGrabInteractable!", this);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _anchor = parentSharedObject.GetAnchorPoint();

        if (IsServer) {
            _isDocked.Value     = startDocked;
            _relativePose.Value = new NetworkPose(transform.localPosition, transform.localRotation);
        }

        _relativePose.OnValueChanged += OnRelativePoseChanged;
        _isDocked.OnValueChanged     += OnDockedStateChanged;

        if (grabInteractable != null) {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        ApplyRelativePose();
        UpdateDockingState(_isDocked.Value);
    }

    private void Update()
    {
        if (IsOwner && (_isBeingGrabbed || !_isDocked.Value))
            UpdateRelativePose();
    }

    private void OnGrabbed(SelectEnterEventArgs args) {
        _isBeingGrabbed = true;
        if (!IsOwner) parentSharedObject.RequestOwnershipServerRpc();
    }

    private void OnReleased(SelectExitEventArgs args) {
        _isBeingGrabbed = false;
        if (IsOwner && !IsServer) parentSharedObject?.ReturnOwnershipServerRpc();

        if (enableAutoUndock && _isDocked.Value)
            CheckAutoUndock();
    }

    private void CheckAutoUndock()
    {
        if (_anchor == null) return;

        float distance = Vector3.Distance(transform.position, _anchor.position);
        
        if (_isDocked.Value && distance > autoUndockDistance)
            SetDockedServerRpc(false);
    }

    public void SetDocked(bool docked) { SetDockedServerRpc(docked); }

    private void UpdateRelativePose()
    {
        if (_anchor == null) return;

        Vector3 newRelativePos    = _anchor.InverseTransformPoint(transform.position);
        Quaternion newRelativeRot = Quaternion.Inverse(_anchor.rotation) * transform.rotation;

        if (Vector3.Distance(newRelativePos, _relativePose.Value.position) > 0.001f ||
            Quaternion.Angle(newRelativeRot, _relativePose.Value.rotation) > 0.1f) {
            UpdateRelativePoseServerRpc(newRelativePos, newRelativeRot);
        }
    }

    private void ApplyRelativePose()
    {
        if (_anchor == null || _isBeingGrabbed) return;

        Vector3 newPosition    = _anchor.TransformPoint(_relativePose.Value.position);
        Quaternion newRotation = _anchor.rotation * _relativePose.Value.rotation;

        transform.SetPositionAndRotation(newPosition, newRotation);
    }

    private void UpdateDockingState(bool isDocked) {
        transform.SetParent(isDocked ? _anchor : parentSharedObject.transform);
    }

    private void OnRelativePoseChanged(NetworkPose oldValue, NetworkPose newValue) {
        if (!IsOwner) ApplyRelativePose();
    }

    private void OnDockedStateChanged(bool oldValue, bool newValue)
    {
        UpdateDockingState(newValue);
        OnDockedChanged?.Invoke(newValue);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateRelativePoseServerRpc(Vector3 position, Quaternion rotation) {
        _relativePose.Value = new NetworkPose(position, rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetDockedServerRpc(bool docked) {
        _isDocked.Value = docked;
    }

    public override void OnNetworkDespawn()
    {
        _relativePose.OnValueChanged -= OnRelativePoseChanged;
        _isDocked.OnValueChanged     -= OnDockedStateChanged;

        if (grabInteractable != null) {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        base.OnNetworkDespawn();
    }
}