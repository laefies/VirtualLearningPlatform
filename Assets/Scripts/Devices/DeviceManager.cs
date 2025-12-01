using UnityEngine;
using Unity.Netcode;
using System.Collections;

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

        Camera cam = Camera.main;
        cam.cullingMask &= ~LayerMask.GetMask("Hidden");
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

    public bool IsAR() => _deviceInfo?.deviceType == DeviceType.AR;
    public bool IsVR() => _deviceInfo?.deviceType == DeviceType.VR;
}