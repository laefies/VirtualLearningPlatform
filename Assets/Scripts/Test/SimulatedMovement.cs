using UnityEngine;

public class SimulatedPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;

    void Update()
    {
        // Movement is achieved using WASD keys
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}