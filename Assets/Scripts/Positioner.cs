using UnityEngine;

public class Positioner : MonoBehaviour
{
    [SerializeField] public Transform target;
    [SerializeField] public bool alwaysFaceCamera = true;
    private Transform xrCamera;

    void Start() {
        xrCamera = Camera.main.transform;
        if (target == null || xrCamera == null) return;
    }

    void LateUpdate() {
        if (target == null || xrCamera == null) return;

        transform.position = target.position;

        if (alwaysFaceCamera) {
            Vector3 lookDir = xrCamera.position - transform.position;
            lookDir.y = 0;
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}
