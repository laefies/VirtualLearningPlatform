using Nova;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Modern OpenXR input handler for Nova UI interactions using Unity's Input System
/// Supports both solid colors and gradient visual feedback with dynamic line length
/// </summary>
public class NovaXRInputHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    [Tooltip("The XR Ray Interactor on this controller")]
    private XRRayInteractor rayInteractor;
    
    [SerializeField]
    [Tooltip("The XR Interactor Line Visual for ray rendering")]
    private XRInteractorLineVisual lineVisual;
    
    [SerializeField]
    [Tooltip("Input action for the select/click button")]
    private InputActionReference selectAction;
    
    [Header("Settings")]
    [SerializeField]
    [Tooltip("Maximum raycast distance for Nova UI interaction")]
    private float maxRayDistance = 100f;
    
    [SerializeField]
    [Tooltip("Unique control ID for this controller (0 for left, 1 for right)")]
    private uint controlID = 0;
    
    [Header("Line Length Settings")]
    [SerializeField]
    [Tooltip("Adjust line length to hit point when hovering Nova UI")]
    private bool adjustLineLength = true;
    
    [Header("Visual Feedback Mode")]
    [SerializeField]
    [Tooltip("Use gradients instead of solid colors")]
    private bool useGradients = false;
    
    [Header("Solid Color Feedback")]
    [SerializeField]
    [Tooltip("Color when hovering over Nova UI")]
    private Color novaHoverColor = new Color(0.2f, 0.8f, 1f);
    
    [SerializeField]
    [Tooltip("Color when clicking Nova UI")]
    private Color novaSelectColor = new Color(0f, 1f, 0.5f);
    
    [SerializeField]
    [Tooltip("Color when not interacting with anything")]
    private Color defaultColor = Color.white;
    
    [Header("Gradient Feedback")]
    [SerializeField]
    [Tooltip("Gradient when hovering over Nova UI")]
    private Gradient novaHoverGradient;
    
    [SerializeField]
    [Tooltip("Gradient when clicking Nova UI")]
    private Gradient novaSelectGradient;
    
    [Header("Haptic Feedback")]
    [SerializeField]
    [Tooltip("Enable haptic feedback on Nova UI interaction")]
    private bool enableHaptics = true;
    
    [SerializeField]
    [Tooltip("Haptic intensity (0-1)")]
    [Range(0f, 1f)]
    private float hapticIntensity = 0.3f;
    
    private bool isSelecting = false;
    private bool wasSelectingLastFrame = false;
    private bool isHoveringNova = false;
    private float originalLineLength = 10f;
    private Gradient originalValidGradient;
    private Gradient originalInvalidGradient;
    
    private void Awake()
    {
        // Auto-assign if not set
        if (rayInteractor == null)
            rayInteractor = GetComponentInChildren<XRRayInteractor>();
        if (lineVisual == null)
            lineVisual = GetComponentInChildren<XRInteractorLineVisual>();
        
        // Store original gradients and line length
        if (lineVisual != null)
        {
            originalValidGradient = CloneGradient(lineVisual.validColorGradient);
            originalInvalidGradient = CloneGradient(lineVisual.invalidColorGradient);
            originalLineLength = lineVisual.minLineLength;
        }
        
        // Initialize default gradients if using gradient mode
        InitializeDefaultGradients();
    }
    
    private void InitializeDefaultGradients()
    {
        if (!useGradients) return;
        
        // Create default hover gradient (cyan to blue)
        if (novaHoverGradient == null || novaHoverGradient.colorKeys.Length == 0)
        {
            novaHoverGradient = new Gradient();
            novaHoverGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.2f, 0.8f, 1f), 0f),
                    new GradientColorKey(new Color(0f, 0.4f, 1f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.6f, 1f)
                }
            );
        }
        
        // Create default select gradient (green to cyan)
        if (novaSelectGradient == null || novaSelectGradient.colorKeys.Length == 0)
        {
            novaSelectGradient = new Gradient();
            novaSelectGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0f, 1f, 0.5f), 0f),
                    new GradientColorKey(new Color(0f, 0.8f, 0.8f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.5f, 1f)
                }
            );
        }
    }
    
    private void OnEnable()
    {
        if (selectAction != null)
        {
            selectAction.action.Enable();
            selectAction.action.performed += OnSelectPerformed;
            selectAction.action.canceled += OnSelectCanceled;
        }
    }
    
    private void OnDisable()
    {
        if (selectAction != null)
        {
            selectAction.action.performed -= OnSelectPerformed;
            selectAction.action.canceled -= OnSelectCanceled;
            selectAction.action.Disable();
        }
        
        // Restore original colors and line length
        RestoreOriginalSettings();
    }
    
    private void OnSelectPerformed(InputAction.CallbackContext context)
    {
        isSelecting = true;
        
        // Haptic feedback when pressing
        if (enableHaptics && isHoveringNova)
        {
            SendHapticFeedback(hapticIntensity, 0.1f);
        }
    }
    
    private void OnSelectCanceled(InputAction.CallbackContext context)
    {
        isSelecting = false;
    }
    
    private void Update()
    {
        // Only process Nova interaction when not selecting other XR objects
        if (!rayInteractor.hasSelection)
        {
            ProcessNovaInteraction();
        }
        else
        {
            // If we were interacting with Nova but now selecting XR object, cancel Nova interaction
            if (wasSelectingLastFrame || isHoveringNova)
            {
                Interaction.Point(new Interaction.Update(GetRay(), controlID), false);
                wasSelectingLastFrame = false;
                isHoveringNova = false;
                RestoreOriginalSettings();
            }
        }
    }
    
    private void ProcessNovaInteraction()
    {
        Ray ray = GetRay();
        
        // Check if we're hovering over Nova UI using a raycast
        RaycastHit hit;
        bool hitNovaUI = CheckNovaUIHit(ray, out hit);
        
        // Send interaction update to Nova
        Interaction.Point(new Interaction.Update(ray, controlID), isSelecting);
        
        // Update visual feedback and line length
        if (adjustLineLength && hitNovaUI)
        {
            float hitDistance = hit.distance * 0.95f;
            UpdateLineLengthToTarget(hitDistance);
        }
        else
        {
            RestoreOriginalLineLength();
        }
        
        UpdateVisualFeedback(hitNovaUI);
        
        // Track state changes for haptic feedback
        if (hitNovaUI && !isHoveringNova && enableHaptics)
        {
            // Just started hovering - light haptic feedback
            SendHapticFeedback(hapticIntensity * 0.5f, 0.05f);
        }
        
        isHoveringNova = hitNovaUI;
        wasSelectingLastFrame = isSelecting;
    }
    
    private bool CheckNovaUIHit(Ray ray, out RaycastHit hit)
    {
        // Perform raycast to check if we're hitting Nova UI
        if (Physics.Raycast(ray, out hit, maxRayDistance))
        {
            // Check if hit object has Nova UI components
            var uiBlock = hit.collider.GetComponentInParent<UIBlock>();
            return uiBlock != null;
        }
        
        hit = default(RaycastHit);
        return false;
    }
    
    private void UpdateLineLengthToTarget(float distance)
    {
        if (lineVisual != null)
        {
            lineVisual.minLineLength = distance;
        }
    }
    
    private void RestoreOriginalLineLength()
    {
        if (lineVisual != null)
        {
            lineVisual.minLineLength = originalLineLength;
        }
    }
    
    private void UpdateVisualFeedback(bool hitNovaUI)
    {
        if (lineVisual == null) return;
        
        if (hitNovaUI)
        {
            if (useGradients)
            {
                // Use gradient based on interaction state
                Gradient targetGradient = isSelecting ? novaSelectGradient : novaHoverGradient;
                SetLineGradient(targetGradient);
            }
            else
            {
                // Use solid color based on interaction state
                Color targetColor = isSelecting ? novaSelectColor : novaHoverColor;
                SetLineColor(targetColor);
            }
        }
        else
        {
            // Restore default color when not hovering Nova UI
            RestoreOriginalColors();
        }
    }
    
    private void SetLineColor(Color color)
    {
        if (lineVisual == null) return;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        
        lineVisual.validColorGradient = gradient;
        lineVisual.invalidColorGradient = gradient;
    }
    
    private void SetLineGradient(Gradient gradient)
    {
        if (lineVisual == null || gradient == null) return;
        
        lineVisual.validColorGradient = CloneGradient(gradient);
        lineVisual.invalidColorGradient = CloneGradient(gradient);
    }
    
    private void RestoreOriginalColors()
    {
        if (lineVisual != null && originalValidGradient != null)
        {
            lineVisual.validColorGradient = originalValidGradient;
            lineVisual.invalidColorGradient = originalInvalidGradient;
        }
    }
    
    private void RestoreOriginalSettings()
    {
        RestoreOriginalColors();
        RestoreOriginalLineLength();
    }
    
    private Ray GetRay()
    {
        // Use the ray from the XR Ray Interactor
        Transform rayOrigin = rayInteractor != null ? rayInteractor.rayOriginTransform : transform;
        return new Ray(rayOrigin.position, rayOrigin.forward * maxRayDistance);
    }
    
    private void SendHapticFeedback(float intensity, float duration)
    {
        // Send haptic impulse to the controller
        if (rayInteractor != null && rayInteractor.xrController != null)
        {
            rayInteractor.xrController.SendHapticImpulse(intensity, duration);
        }
    }
    
    private Gradient CloneGradient(Gradient original)
    {
        if (original == null) return null;
        
        Gradient clone = new Gradient();
        clone.SetKeys(original.colorKeys, original.alphaKeys);
        clone.mode = original.mode;
        return clone;
    }
    
    private void OnValidate()
    {
        if (rayInteractor == null)
            rayInteractor = GetComponentInChildren<XRRayInteractor>();
        if (lineVisual == null)
            lineVisual = GetComponentInChildren<XRInteractorLineVisual>();
        
        // Initialize default gradients if switching to gradient mode
        if (useGradients)
        {
            InitializeDefaultGradients();
        }
    }
}