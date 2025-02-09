using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPositioner : MonoBehaviour
{
    [SerializeField]
    public Transform target;

    [SerializeField]
    public float distance = 1.0f;

    private Transform xrCamera;

    void Start()
    {
        xrCamera = FindObjectOfType<Camera>().transform;
    }

    void LateUpdate()
    {
        if (target == null || xrCamera == null) return;

        Vector3 cameraRight = xrCamera.right;
        cameraRight.y = 0;
        cameraRight.Normalize(); 
        
        transform.position = target.position + cameraRight * distance;

        Vector3 lookDirection = xrCamera.position - transform.position;
        lookDirection.y = 0;
        transform.rotation = Quaternion.LookRotation(lookDirection);
    }

}