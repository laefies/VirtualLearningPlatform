using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Positioner : MonoBehaviour
{
    [SerializeField]
    public Transform target;

    [SerializeField]
    public float distance = 1.0f;

    [SerializeField]
    public bool alwaysFaceCamera = true;

    [SerializeField]
    public PositionRelativeToCamera positionRelative = PositionRelativeToCamera.Right;

    private Transform xrCamera;

    public enum PositionRelativeToCamera
    {
        Right,
        Left,
        Above,
        Below
    }

    void Start()
    {
        xrCamera = FindObjectOfType<Camera>().transform;
    }

    void LateUpdate()
    {
        if (target == null || xrCamera == null) return;

        Vector3 offset = Vector3.zero;
        switch (positionRelative)
        {
            case PositionRelativeToCamera.Right:
                offset = xrCamera.right;
                break;
            case PositionRelativeToCamera.Left:
                offset = -xrCamera.right;
                break;
            case PositionRelativeToCamera.Above:
                offset = xrCamera.up;
                break;
            case PositionRelativeToCamera.Below:
                offset = -xrCamera.up;
                break;
        }

        offset.y = 0; 
        offset.Normalize();
        transform.position = target.position + offset * distance;

        if (alwaysFaceCamera)
        {
            Vector3 lookDirection = xrCamera.position - transform.position;
            lookDirection.y = 0;
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
        //     else
        // {
        //     Vector3 targetForward = target.forward;
        //     targetForward.y = 0;
        //     transform.rotation = Quaternion.LookRotation(targetForward);
        // }
    }
}