using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

public class BodyLockedUI : MonoBehaviour
{
    [Header("UI Positioning")]
    [Tooltip("Distance in front of the player")]
    [SerializeField] private float forwardDistance = 0.375f;
    
    [Tooltip("Height offset relative to head (0 = at eye level, negative = below)")]
    [SerializeField] private float heightOffset = 0.03f;
    
    [Tooltip("Tilt angle of the UI (positive = angled down toward player)")]
    [SerializeField] private float tiltAngle = -15f;

    [Header("Repositioning Thresholds")]
    [Tooltip("Max distance the player can move before UI repositions")]
    [SerializeField] private float maxDistance = 0.4f;
    
    [Tooltip("Max rotation angle (degrees) before UI repositions")]
    [SerializeField] private float maxRotationAngle = 45f;

    [Header("Animation")]
    [SerializeField] private float lerpSpeed = 2f;

    [Header("XR Setup")]
    private XROrigin xrOrigin;
    [SerializeField] private InputActionReference headPositionAction;
    [SerializeField] private InputActionReference headRotationAction;

    // Tracking variables
    private Vector3 lastRepositionPosition;
    private Quaternion lastRepositionRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    protected void Start()
    {
        xrOrigin = FindObjectOfType<XROrigin>();
        Debug.Log(transform.position - GetHeadPose().position);
        RepositionUI();
    }

    protected void Update()
    {        
        if ( ShouldReposition(GetHeadPose()) )
            RepositionUI();

        // Lerp to target position/rotation
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
    }

    bool ShouldReposition(Pose headPose)
    {
        // Check distance threshold
        float distanceMoved = Vector3.Distance(headPose.position, lastRepositionPosition);
        if (distanceMoved > maxDistance)
            return true;

        // Check rotation threshold
        float angleDifference = Quaternion.Angle(
            Quaternion.Euler(0, headPose.rotation.eulerAngles.y, 0),
            Quaternion.Euler(0, lastRepositionRotation.eulerAngles.y, 0)
        );
        
        if (angleDifference > maxRotationAngle)
            return true;

        return false;
    }

    void RepositionUI()
    {
        Pose headPose = GetHeadPose();
        
        // Store current head pose as the new reference point
        lastRepositionPosition = headPose.position;
        lastRepositionRotation = headPose.rotation;

        // Calculate target position
        Vector3 headForward = headPose.rotation * Vector3.forward;
        headForward.y = 0;
        headForward.Normalize();

        // Line 1 -> Start at head position
        // Line 2 -> Move forward (towards where the user is facing) by 'forwardDistance'
        // Line 3 -> Move directly up based on 'heightOffset'
        targetPosition = headPose.position 
            + headForward * forwardDistance 
            + Vector3.up * heightOffset;

        // Calculate target rotation
        Vector3 directionToPlayer = headPose.position - targetPosition;
        directionToPlayer.y = 0;
        
        if (directionToPlayer != Vector3.zero)
            targetRotation = Quaternion.LookRotation(directionToPlayer) * Quaternion.Euler(tiltAngle, 0, 0);
    }

    Pose GetHeadPose()
    {
        if (xrOrigin != null && headPositionAction != null && headRotationAction != null)
        {
            Transform originTransform = xrOrigin.CameraFloorOffsetObject.transform;
            Vector3 headPos = headPositionAction.action.ReadValue<Vector3>();
            Quaternion headRot = headRotationAction.action.ReadValue<Quaternion>();
            
            return new Pose(
                originTransform.TransformPoint(headPos), 
                originTransform.rotation * headRot
            );
        }
        
        // Fallback to main camera if working with simulator rather than XR Device
        return new Pose(Camera.main.transform.position, Camera.main.transform.rotation);
    }

}