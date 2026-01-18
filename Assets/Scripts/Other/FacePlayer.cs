using UnityEngine;

public class Positioner : MonoBehaviour
{
    [SerializeField] public Transform target;
    private Transform _player;

    void Start() {
        _player = Camera.main.transform;
        if (_player == null) return;
    }

    void LateUpdate() {
        if (_player == null) return;

        // Keep attached to the target position
        if (target != null) transform.position = target.position;

        // Face player while staying horizontally aligned
        Vector3 targetDirection = _player.position - transform.position; 
        targetDirection.y = 0;
        transform.rotation = Quaternion.LookRotation(targetDirection);
    }
}
