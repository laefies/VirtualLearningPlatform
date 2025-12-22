using UnityEngine;

/// <summary>
/// Controls simulated player movement in a virtual environment.
/// Provides basic WASD movement mechanics for testing and development
/// without requiring any physical VR hardware.
/// </summary>
public class SimulatedPlayerController : MonoBehaviour
{
    /// <summary>
    /// Movement speed multiplier - the higher the value, the faster the player moves.
    /// </summary>
    [SerializeField] private float moveSpeed = 0.35f;

    /// <summary>
    /// Handling all player movement based on keyboard input.
    /// </summary>
    void Update()
    {
        // Get input values:
        //    :: "Horizontal" axis corresponds to A (negative) and D (positive) keys
        //    :: "Vertical"   axis corresponds to S (negative) and W (positive) keys
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Create the movement vector based on the player's current orientation
        // this makes movement relative to where the player is facing
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Apply the movement
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}
