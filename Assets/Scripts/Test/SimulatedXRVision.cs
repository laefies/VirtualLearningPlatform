using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatedXRVision : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform cameraTransform;

    float pitch = 0f;

    void Update()
    {
        MoveCamera();
        HandleSimulatedSpawns();
    }

    void MoveCamera() {
        // Mouse is used to look around with the camera
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // In order to keep the cursor locked in the center, the mode is changed
        Cursor.lockState = CursorLockMode.Locked;
    }

    void HandleSimulatedSpawns() {
        if (Input.GetKey(KeyCode.P)) {
            NetworkObjectManager.Instance.ProcessMarkerServerRpc(
                new MarkerInfo {
                    Id   = "Solar Panel",
                    Pose = new Pose(transform.position + transform.forward * 0.25f, Quaternion.identity),
                    Size = 0.05f
                }
            );
        }
    }
}
