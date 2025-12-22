using UnityEngine;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;

/// <summary>
/// Represents the local player/device.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class LocalPlayer : NetworkBehaviour
{
    public static LocalPlayer Instance { get; private set; }

    [Header("Device Configuration")]
    private DeviceData deviceData;

    [Header("XR Tracking")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private InputActionReference headPositionAction;
    [SerializeField] private InputActionReference headRotationAction;

    [Header("Player Info")]
    private string playerName;

    public string PlayerName => playerName;
    public DeviceType DeviceType => deviceData != null ? deviceData.deviceType : DeviceType.Other;
    public bool IsAR => deviceData != null && deviceData.deviceType == DeviceType.AR;
    public bool IsVR => deviceData != null && deviceData.deviceType == DeviceType.VR;

    void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (xrOrigin == null)
            xrOrigin = FindObjectOfType<XROrigin>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Initialize the player with device-specific configuration.
    /// </summary>
    public void Initialize(DeviceData data, string name)
    {
        deviceData = data;
        playerName = name;

        Debug.Log($"[LocalPlayer] Initialized as '{data.deviceName}' ({data.deviceType})");

        // Configure camera culling
        if (Camera.main != null)
            Camera.main.cullingMask &= ~LayerMask.GetMask("Hidden");
    }

    /// <summary>
    /// Gets the current head pose in world space.
    /// </summary>
    public Pose GetHeadPose()
    {
        // XR Device tracking
        if (xrOrigin != null && headPositionAction != null && headRotationAction != null)
        {
            Transform origin = xrOrigin.CameraFloorOffsetObject.transform;
            Vector3 localPos = headPositionAction.action.ReadValue<Vector3>();
            Quaternion localRot = headRotationAction.action.ReadValue<Quaternion>();

            return new Pose(
                origin.TransformPoint(localPos),
                origin.rotation * localRot
            );
        }

        // Fallback to main camera (for simulator/editor)
        if (Camera.main != null)
            return new Pose(Camera.main.transform.position, Camera.main.transform.rotation);

        return Pose.identity;
    }

    void FixedUpdate()
    {
        if (IsVR) ApplyGravity();
    }

    private void ApplyGravity()
    {
        CharacterController controller = GetComponentInChildren<CharacterController>();
        if (controller != null)
        {
            if (!controller.isGrounded)
                controller.Move(Vector3.down * 9.81f * Time.fixedDeltaTime);
            return;
        }

        Rigidbody rb = GetComponentInChildren<Rigidbody>();
        rb?.AddForce(Vector3.down * 9.81f, ForceMode.Acceleration);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();        
        Debug.Log($"[LocalPlayer] Network spawned. IsOwner: {IsOwner}, IsServer: {IsServer}");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Debug.Log("[LocalPlayer] Network despawned.");
    }
}