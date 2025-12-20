using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simulates XR vision and spawning capabilities for development and testing.
/// Provides mouse-based camera control and simulated object placement without
/// requiring physical XR hardware.
/// </summary>
public class SimulatedXRVision : MonoBehaviour
{
    /// <summary>
    /// Controls how fast the camera rotates in response to mouse movement.
    /// </summary>
    [SerializeField] private float mouseSensitivity = 100f;
    
    /// <summary>
    /// Reference to the camera transform that will be rotated for looking up and down.
    /// </summary>
    [SerializeField] private Transform cameraTransform;

    /// <summary>
    /// Stores the vertical rotation angle (looking up and down).
    /// </summary>
    private float pitch = 0f;

    /// <summary>
    /// Handles camera movement and object spawning simulations.
    /// </summary>
    void Update()
    {
        MoveCamera();        
    }

    /// <summary>
    /// Simulates head movement in VR using mouse input.
    /// </summary>
    void MoveCamera() {
        // Calculate rotation amounts based on mouse movement
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Update the pitch angle (up/down rotation)
        // Note :: mouseY value is subtracted to correctly map "mouse up" to rotating the view up
        pitch -= mouseY;
        
        // Clamp the pitch to prevent the camera from "flipping over"
        pitch = Mathf.Clamp(pitch, -90f, 90f); // (restricted between -90° to +90°)

        // Apply the pitch rotation to the camera
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        
        // Apply the yaw rotation to the player object
        transform.Rotate(Vector3.up * mouseX);

        // Prevent the cursor from leaving the game window while rotating
        Cursor.lockState = CursorLockMode.Locked;
    }
}