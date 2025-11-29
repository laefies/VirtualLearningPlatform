using UnityEngine;
using Unity.Netcode;

// Responsible for storing information regarding the device, and handle its subsystems.
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(VirtualPlacementSystem))]
public class DeviceManager : MonoBehaviour
{
    private DeviceInfo _deviceInfo;

    public static DeviceManager Instance { get; private set; }

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }    

    void Start() { SceneLoader.Instance.OnSceneLoaded += PrepareDeviceForScene; }
    void OnDestroy() { SceneLoader.Instance.OnSceneLoaded -= PrepareDeviceForScene; }

    // Called during startup - assigns the manager to a specified DeviceInfo.
    public void Initialize(DeviceInfo info) {
        _deviceInfo = info;
        Debug.Log("[Device Manager] Starting! Device recognized as '" + info.deviceName + "'.");
    }


    void PrepareDeviceForScene(object sender, SceneLoader.SceneEventArgs e) {
        if (!IsAR() && e.sceneInfo.vrEnvironmentPrefab != null) {
            Instantiate(e.sceneInfo.vrEnvironmentPrefab);
            GroundCharacterController();
        }
    }

    void GroundCharacterController() {
        CharacterController controller = GetComponentInChildren<CharacterController>();
        if (controller == null) return;
        
        RaycastHit hit;
        float rayDistance = 100f;
        Vector3 rayOrigin = transform.position + Vector3.up;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance)) {
            float groundOffset = controller.height / 2f + controller.skinWidth - controller.center.y;
            transform.position = hit.point + Vector3.up * groundOffset;
        }
        
        // Force collisions to reset
        controller.enabled = false;
        controller.enabled = true;
        controller.Move(Vector3.down * 0.1f);
    }

    public bool IsAR() => _deviceInfo?.deviceType == DeviceType.AR;
    public bool IsVR() => _deviceInfo?.deviceType == DeviceType.VR;
}