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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
    }

    public Pose ToPose() => new Pose(position, rotation);
}

public abstract class Interactable : NetworkBehaviour
{
    [SerializeField] private float dockTolMultiplier = 1.5f;

    protected Spawnable spawnable;
    private NetworkVariable<NetworkPose> _relativePose = new NetworkVariable<NetworkPose>(new NetworkPose(Vector3.zero, Quaternion.identity), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        spawnable = GetComponentInParent<Spawnable>();
        PrepareComponents();

        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.onSelectEntered.AddListener(OnGrab);
        grabInteractable.onSelectExited.AddListener(OnRelease);

        _relativePose.OnValueChanged += (oldValue, newValue) => { 
            if (!IsOwner) { transform.SetPositionAndRotation(
                    spawnable.transform.TransformPoint(newValue.position),
                    spawnable.transform.rotation * newValue.rotation ); } };
    }

    private void OnGrab(XRBaseInteractor interactor)
    {
        if (!spawnable.GetComponent<NetworkObject>().IsOwner) {
            spawnable.RequestOwnershipServerRpc();
        }
    }

    private void OnRelease(XRBaseInteractor interactor)
    {
        spawnable.ReturnOwnershipServerRpc();
        // CheckAutoUndock();
    }

    public void CheckAutoUndock()
    {
        float distance = Vector3.Distance(transform.position, spawnable.transform.position);

        if (distance > spawnable.transform.localScale.x * dockTolMultiplier)
            spawnable.ChangeDockStatus(false);
    }

    void Update()
    {
        if (IsOwner) {
            _relativePose.Value = new NetworkPose(spawnable.transform.InverseTransformPoint(transform.position),
                                           Quaternion.Inverse(spawnable.transform.rotation) * transform.rotation);
        }

        try { UpdateComponents(); } catch { /**/ }
    }

    public abstract void PrepareComponents();
    public abstract void UpdateComponents();
}