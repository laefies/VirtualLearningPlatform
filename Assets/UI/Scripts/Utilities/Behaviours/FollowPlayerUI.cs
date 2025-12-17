using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Nova;

/// <summary>
/// Makes UI follow the player's head with smooth repositioning for comfortable XR viewing.
/// Supports manual repositioning via drag handle.
/// </summary>
public class FollowPlayerUI : MonoBehaviour
{
    [Header("UI Positioning")]
    [Tooltip("Distance in front of the player")]
    [SerializeField] private float _forwardDistance = 0.375f;
    
    [Tooltip("Height offset relative to head (0 = eye level, negative = below)")]
    [SerializeField] private float _heightOffset = 0.03f;
    
    [Tooltip("Tilt angle of the UI")]
    [SerializeField] private float _tiltAngle = 15f;

    [Header("Repositioning Thresholds")]
    [Tooltip("Max distance the player can move before UI repositions")]
    [SerializeField] private float _maxDistance = 0.4f;
    
    [Tooltip("Max rotation angle (in degrees) before UI repositions")]
    [SerializeField] private float _maxRotationAngle = 45f;

    [Header("Animation")]
    [Tooltip("Speed of position/rotation interpolation")]
    [SerializeField] private float _lerpSpeed = 2f;
    
    [Tooltip("Enable scale animation on spawn")]
    [SerializeField] private bool _enableSpawnAnimation = true;
    
    [Tooltip("Duration of spawn scale animation")]
    [SerializeField] private float _spawnAnimationDuration = 0.3f;
    
    [Tooltip("Animation curve for spawn scale")]
    [SerializeField] private AnimationCurve _spawnScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Sound to play when UI spawns")]
    [SerializeField] private AudioClip _spawnSound;
    
    [Tooltip("Volume of spawn sound (0-1)")]
    [SerializeField] private float _spawnSoundVolume = 1f;

    [Header("Manual Repositioning")]
    [Tooltip("Allow user to manually reposition UI")]
    [SerializeField] private bool _allowManualReposition = true;
    
    [Tooltip("Handle object with which the user may manually reposition UI")]
    [SerializeField] private XRGrabInteractable _manualRepositionHandle;
    
    [Tooltip("Lock rotation axes during manual reposition (only allow tilt)")]
    [SerializeField] private bool _lockRotationAxes = true;

    // Cached values
    private Transform _transform;
    private DeviceManager _deviceManager;
    private UIBlock _novaUIBlock;

    // Single tracking state
    private Pose _lastHeadPose;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;

    // Manual repositioning state
    private bool _isBeingManuallyRepositioned;
    private Vector3 _manualRepositionStartPos;
    private Quaternion _manualRepositionStartRot;
    
    // Spawn animation state
    private bool _isAnimatingSpawn;
    private float _spawnAnimationTime;
    private Vector3 _targetScale;

    protected void Awake()
    {
        _transform = transform;
        _deviceManager = DeviceManager.Instance;
        _manualRepositionHandle ??= GetComponent<XRGrabInteractable>();
        _novaUIBlock ??= GetComponent<UIBlock>();
        
        // Store the target scale and start at zero if animation is enabled
        _targetScale = _transform.localScale;
        if (_enableSpawnAnimation)
        {
            _transform.localScale = Vector3.zero;
            _isAnimatingSpawn = true;
            _spawnAnimationTime = 0f;
        }
        
        ForceReposition();
    }

    protected virtual void OnEnable()
    {
        _manualRepositionHandle?.selectEntered.AddListener(OnGrabStarted);
        _manualRepositionHandle?.selectExited.AddListener(OnGrabEnded);
    }

    protected virtual void OnDisable()
    {
        _manualRepositionHandle?.selectEntered.RemoveListener(OnGrabStarted);
        _manualRepositionHandle?.selectExited.RemoveListener(OnGrabEnded);
    }

    protected virtual void Start()
    {
        RepositionUI();
    }

    protected virtual void Update()
    {
        // Handle spawn animation
        if (_isAnimatingSpawn)
        {
            _spawnAnimationTime += Time.deltaTime;
            float t = Mathf.Clamp01(_spawnAnimationTime / _spawnAnimationDuration);
            float curveValue = _spawnScaleCurve.Evaluate(t);
            _transform.localScale = _targetScale * curveValue;
            
            if (t >= 1f)
            {
                _isAnimatingSpawn = false;
                _transform.localScale = _targetScale;
            }
        }
        
        // Skip automatic repositioning while being manually moved
        if (_isBeingManuallyRepositioned) return;

        Pose headPose = _deviceManager.GetHeadPose();
        if (ShouldReposition(headPose))
            RepositionUI(headPose);

        // Smooth interpolation
        float deltaTime = Time.deltaTime;
        _transform.position = Vector3.Lerp(_transform.position, _targetPosition, deltaTime * _lerpSpeed);
        _transform.rotation = Quaternion.Slerp(_transform.rotation, _targetRotation, deltaTime * _lerpSpeed);
    }

    protected virtual void LateUpdate()
    {
        if (!_isBeingManuallyRepositioned || !_lockRotationAxes) return;

        Pose headPose = _deviceManager.GetHeadPose();
        Vector3 toPlayerFlat = headPose.position - _transform.position;
        toPlayerFlat.y = 0f;

        if (toPlayerFlat.sqrMagnitude > 0.001f)
        {
            toPlayerFlat.Normalize();
            Quaternion baseRotation = Quaternion.LookRotation(-toPlayerFlat);
            Vector3 currentEuler = _transform.rotation.eulerAngles;
            Vector3 baseEuler = baseRotation.eulerAngles;
            float tiltAngle = Mathf.DeltaAngle(baseEuler.x, currentEuler.x);
            _transform.rotation = baseRotation * Quaternion.Euler(tiltAngle, 0f, 0f);
        }
    }

    private bool ShouldReposition(Pose headPose)
    {
        // Distance check using sqrMagnitude (faster than Distance)
        float sqrDistance = (headPose.position - _lastHeadPose.position).sqrMagnitude;
        if (sqrDistance > _maxDistance * _maxDistance)
            return true;

        // Y-axis rotation check
        float lastY = _lastHeadPose.rotation.eulerAngles.y;
        float currentY = headPose.rotation.eulerAngles.y;
        float angleDelta = Mathf.DeltaAngle(lastY, currentY);

        return Mathf.Abs(angleDelta) > _maxRotationAngle;
    }

    private void RepositionUI()
    {
        RepositionUI(_deviceManager.GetHeadPose());
    }

    private void RepositionUI(Pose headPose)
    {
        // Update tracking state
        _lastHeadPose = headPose;

        // Calculate forward direction (ignore vertical component)
        Vector3 headForward = headPose.rotation * Vector3.forward;
        headForward.y = 0f;
        if (headForward.sqrMagnitude > 0.001f)
            headForward.Normalize();
        else
            headForward = Vector3.forward;

        // Calculate target position
        _targetPosition = headPose.position + headForward * _forwardDistance + Vector3.up * _heightOffset;

        // Calculate target rotation (look at player with tilt)
        Vector3 toPlayer = _targetPosition - headPose.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.001f)
            _targetRotation = Quaternion.LookRotation(toPlayer) * Quaternion.Euler(_tiltAngle, 0f, 0f);
        else
            _targetRotation = Quaternion.Euler(_tiltAngle, 0f, 0f);
    }

    private void OnGrabStarted(SelectEnterEventArgs args)
    {
        BeginManualReposition();
    }

    private void OnGrabEnded(SelectExitEventArgs args)
    {
        EndManualReposition();
    }

    public void BeginManualReposition()
    {
        if (!_allowManualReposition) return;

        _isBeingManuallyRepositioned = true;
        _manualRepositionStartPos = _transform.position;
        _manualRepositionStartRot = _transform.rotation;
    }

    public void EndManualReposition()
    {
        if (!_isBeingManuallyRepositioned) return;

        _isBeingManuallyRepositioned = false;

        // Calculate new parameters from current transform
        Pose headPose = _deviceManager.GetHeadPose();

        // Calculate new forward distance
        Vector3 headForward = headPose.rotation * Vector3.forward;
        headForward.y = 0f;
        if (headForward.sqrMagnitude > 0.001f)
            headForward.Normalize();
        else
            headForward = Vector3.forward;

        Vector3 toUI = _transform.position - headPose.position;
        _forwardDistance = Vector3.Dot(toUI, headForward);
        _forwardDistance = Mathf.Max(0.1f, _forwardDistance); // Clamp minimum distance

        // Calculate new height offset
        _heightOffset = _transform.position.y - headPose.position.y;

        // Calculate new tilt angle
        Vector3 toPlayerFlat = headPose.position - _transform.position;
        toPlayerFlat.y = 0f;
        if (toPlayerFlat.sqrMagnitude > 0.001f)
        {
            toPlayerFlat.Normalize();
            Quaternion flatRotation = Quaternion.LookRotation(-toPlayerFlat);
            Vector3 flatForward = flatRotation * Vector3.forward;
            Vector3 actualForward = _transform.forward;
            float verticalAngle = Vector3.SignedAngle(
                new Vector3(flatForward.x, 0, flatForward.z).normalized,
                actualForward,
                Vector3.Cross(flatForward, Vector3.up)
            );
            _tiltAngle = -verticalAngle;
        }

        // Update targets to current position
        _targetPosition = _transform.position;
        _targetRotation = _transform.rotation;
        _lastHeadPose = headPose;
    }

    public void CancelManualReposition()
    {
        if (!_isBeingManuallyRepositioned) return;

        _isBeingManuallyRepositioned = false;
        _targetPosition = _manualRepositionStartPos;
        _targetRotation = _manualRepositionStartRot;
    }

    public bool IsBeingManuallyRepositioned => _isBeingManuallyRepositioned;

    public void ForceReposition()
    {
        RepositionUI();
        _novaUIBlock.TrySetWorldPosition(_targetPosition);
        _transform.rotation = _targetRotation;
    }
    
    /// <summary>
    /// Triggers the spawn animation manually (useful if re-enabling the UI)
    /// </summary>
    public void PlaySpawnAnimation()
    {
        if (_enableSpawnAnimation)
        {
            _transform.localScale = Vector3.zero;
            _isAnimatingSpawn = true;
            _spawnAnimationTime = 0f;
        }
    }
}