using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Netcode;

public struct NetworkPose : INetworkSerializable
{
    public Vector3 position;
    public Quaternion rotation;

    public NetworkPose(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
    }

    public Pose ToPose() => new Pose(position, rotation);
}

public abstract class Dockable : NetworkBehaviour {

    [Header("Docking Configuration")]
    [SerializeField] protected Spawnable spawnable;
    [SerializeField] protected XRGrabInteractable grabInteractable;
    [SerializeField] private float autoUndockDistanceMultiplier = 1.5f;
    [SerializeField] private bool enableAutoUndock = true;

    private NetworkVariable<NetworkPose> _relativePose = new NetworkVariable<NetworkPose>(
        new NetworkPose(Vector3.zero, Quaternion.identity),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    protected virtual void Awake()
    {
        if (spawnable == null) 
            spawnable = GetComponentInParent<Spawnable>();
        if (grabInteractable == null) 
            grabInteractable = GetComponent<XRGrabInteractable>();

        ValidateComponents();
        PrepareComponents();
        SetupNetworkCallbacks();
        SetupGrabCallbacks();
    }

    private void ValidateComponents() {
        if (spawnable == null)
            Debug.LogError($"[{gameObject.name}] Spawnable not found! Assign it in the inspector or ensure it's in the parent hierarchy.", this);

        if (grabInteractable == null)
            Debug.LogError($"[{gameObject.name}] XRGrabInteractable not found! Docking behaviour might not work appropriately.", this);
    }

    private void SetupNetworkCallbacks() {
        _relativePose.OnValueChanged += OnRelativePoseChanged;
    }

    private void SetupGrabCallbacks()
    {
        if (grabInteractable != null) {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    private void OnRelativePoseChanged(NetworkPose oldValue, NetworkPose newValue)
    {
        if (!IsOwner && spawnable != null) {
            transform.SetPositionAndRotation(
                spawnable.transform.TransformPoint(newValue.position),
                spawnable.transform.rotation * newValue.rotation
            );
        }
    }

    protected virtual void OnGrab(SelectEnterEventArgs args)
    {
        if (spawnable == null) return;

        NetworkObject netObj = spawnable.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsOwner)
            spawnable.RequestOwnershipServerRpc();

        OnGrabbed(args.interactorObject);
    }

    protected virtual void OnRelease(SelectExitEventArgs args)
    {
        if (spawnable == null) return;

        spawnable.ReturnOwnershipServerRpc();

        // if (enableAutoUndock)
        //     CheckAutoUndock();

        OnReleased(args.interactorObject);
    }

    public void CheckAutoUndock()
    {
        if (spawnable == null) return;

        float distance = Vector3.Distance(transform.position, spawnable.transform.position);
        float undockThreshold = spawnable.transform.localScale.x * autoUndockDistanceMultiplier;

        if (distance > undockThreshold)
            spawnable.ChangeDockStatus(false);
    }

    protected virtual void Update()
    {
        if (IsOwner && spawnable != null) {
            _relativePose.Value = new NetworkPose(
                spawnable.transform.InverseTransformPoint(transform.position),
                Quaternion.Inverse(spawnable.transform.rotation) * transform.rotation
            );
        }

        UpdateComponents();
    }

    // Abstract methods for derived classes
    public abstract void PrepareComponents();
    public abstract void UpdateComponents();

    // Virtual methods for optional override
    protected virtual void OnGrabbed(IXRSelectInteractor interactor) {}
    protected virtual void OnReleased(IXRSelectInteractor interactor) {}

}