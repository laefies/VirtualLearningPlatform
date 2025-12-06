using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;

// Responsible for storing information regarding the device, and handle its subsystems.
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(VirtualPlacementSystem))]
public class DeviceManager : MonoBehaviour
{
    private DeviceInfo _deviceInfo;

    [Header("XR Setup")]
    private XROrigin xrOrigin;
    [SerializeField] private InputActionReference headPositionAction;
    [SerializeField] private InputActionReference headRotationAction;

    public static DeviceManager Instance { get; private set; }

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }    

    void Start() { 
        xrOrigin = FindObjectOfType<XROrigin>();

        SceneLoader.Instance.OnSceneLoaded += PrepareDeviceForScene; 
    }
    void OnDestroy() { SceneLoader.Instance.OnSceneLoaded -= PrepareDeviceForScene; }

    // Called during startup - assigns the manager to a specified DeviceInfo.
    public void Initialize(DeviceInfo info) {
        _deviceInfo = info;
        Debug.Log("[Device Manager] Starting! Device recognized as '" + info.deviceName + "'.");

        Camera cam = Camera.main;
        cam.cullingMask &= ~LayerMask.GetMask("Hidden");
    }

    public string GetDeviceName() {
        return _deviceInfo.deviceName;
    }

    void PrepareDeviceForScene(object sender, SceneLoader.SceneEventArgs e) {
        if (!IsAR() && e.sceneInfo.vrEnvironmentPrefab != null)
            Instantiate(e.sceneInfo.vrEnvironmentPrefab);
    }

    void FixedUpdate() {
        if (!IsAR()) {
            CharacterController controller = GetComponentInChildren<CharacterController>();
            if (controller != null && !controller.isGrounded) 
                controller.Move( Vector3.down * 9.81f * Time.deltaTime );
        }
    }

    public Pose GetHeadPose()
    {
        if (xrOrigin != null && headPositionAction != null && headRotationAction != null)
        {
            Transform originTransform = xrOrigin.CameraFloorOffsetObject.transform;
            Vector3 headPos = headPositionAction.action.ReadValue<Vector3>();
            Quaternion headRot = headRotationAction.action.ReadValue<Quaternion>();
            
            return new Pose(
                originTransform.TransformPoint(headPos), 
                originTransform.rotation * headRot
            );
        }
        
        // Fallback to main camera if working with simulator rather than XR Device
        return new Pose(Camera.main.transform.position, Camera.main.transform.rotation);
    }

    public bool IsAR() => _deviceInfo?.deviceType == DeviceType.AR;
    public bool IsVR() => _deviceInfo?.deviceType == DeviceType.VR;
}