using Nova;
using UnityEngine;

public class InteractionFeedback : MonoBehaviour
{
    [Header("Target Elements")]
    [Tooltip("The UIBlock2D to apply visual changes to (usually the background)")]
    [SerializeField] private UIBlock2D targetBlock;
    [Tooltip("Optional TextBlock to change color")]
    [SerializeField] private TextBlock targetLabel;

    [Header("Hover State")]
    [SerializeField] private bool enableHoverEffects = true;
    [SerializeField] private float hoverDelay = 0f;
    [SerializeField] private VisualStateConfig hoverState = new VisualStateConfig()
    {
        changeBackgroundColor = true,
        backgroundColor = new Color(0.9f, 0.9f, 0.9f),
        changeScale = true,
        scale = new Vector3(1.05f, 1.05f, 1.05f)
    };

    [Header("Pressed State")]
    [SerializeField] private bool enablePressedEffects = true;
    [SerializeField] private VisualStateConfig pressedState = new VisualStateConfig()
    {
        changeBackgroundColor = true,
        backgroundColor = new Color(0.8f, 0.8f, 0.8f),
        changeScale = true,
        scale = new Vector3(0.95f, 0.95f, 0.95f),
        changeBorder = true,
        showBorder = true,
        borderColor = Color.black,
        borderWidth = 2f
    };

    [Header("Disabled State")]
    [SerializeField] private bool enableDisabledState = false;
    [SerializeField] private VisualStateConfig disabledState = new VisualStateConfig()
    {
        changeBackgroundColor = true,
        backgroundColor = new Color(0.7f, 0.7f, 0.7f),
        changeTextColor = true,
        textColor = new Color(0.5f, 0.5f, 0.5f)
    };

    [Header("Animation Settings")]
    [SerializeField] private float transitionSpeed = 10f;
    [SerializeField] private bool useSmoothTransitions = true;
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip pressSound;
    [SerializeField] private AudioClip releaseSound;
    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 0.5f;

    // Runtime state tracking
    private Vector3 targetScale;
    private Color targetBackgroundColor;
    private Color targetTextColor;
    private bool isPressed = false;
    private bool isHovered = false;
    private bool isDisabled = false;
    private bool isAnimating = false;
    private float hoverTimer = 0f;
    private bool hoverDelayElapsed = false;

    // Store original values (cached once at startup)
    private OriginalState originalState;

    // Animation progress tracking
    private float animationProgress = 0f;

    private struct OriginalState
    {
        public Vector3 scale;
        public Color backgroundColor;
        public Color textColor;
        public bool borderEnabled;
        public Color borderColor;
        public float borderWidth;
        public bool shadowEnabled;
        public Color shadowColor;
        public float shadowBlur;
        public Length2 shadowOffset;
        public Length cornerRadius;
    }

    private void Awake()
    {
        // Auto-find components if not assigned
        if (targetBlock == null)
        {
            targetBlock = GetComponentInChildren<UIBlock2D>();
        }
        if (targetLabel == null)
        {
            targetLabel = GetComponentInChildren<TextBlock>();
        }

        CaptureOriginalState();
    }

    private void CaptureOriginalState()
    {
        if (targetBlock == null) return;

        originalState = new OriginalState
        {
            scale = targetBlock.transform.localScale,
            backgroundColor = targetBlock.Color,
            textColor = targetLabel != null ? targetLabel.Color : Color.white,
            borderEnabled = targetBlock.Border.Enabled,
            borderColor = targetBlock.Border.Color,
            borderWidth = targetBlock.Border.Width.Value,
            shadowEnabled = targetBlock.Shadow.Enabled,
            shadowColor = targetBlock.Shadow.Color,
            shadowBlur = targetBlock.Shadow.Blur.Value,
            shadowOffset = targetBlock.Shadow.Offset,
            cornerRadius = targetBlock.CornerRadius
        };

        // Set initial target values
        targetScale = originalState.scale;
        targetBackgroundColor = originalState.backgroundColor;
        targetTextColor = originalState.textColor;
    }

    private void Update()
    {
        // Only run when actively animating
        if (!isAnimating || !useSmoothTransitions || targetBlock == null) return;

        // Update animation progress
        animationProgress += Time.deltaTime * transitionSpeed;
        float t = easingCurve.Evaluate(Mathf.Clamp01(animationProgress));

        // Animate scale
        Vector3 currentScale = targetBlock.transform.localScale;
        Vector3 newScale = Vector3.Lerp(currentScale, targetScale, t);
        
        if (Vector3.Distance(newScale, targetScale) < 0.001f)
        {
            targetBlock.transform.localScale = targetScale;
        }
        else
        {
            targetBlock.transform.localScale = newScale;
        }

        // Animate background color
        Color currentBg = targetBlock.Color;
        Color newBg = Color.Lerp(currentBg, targetBackgroundColor, t);
        
        if (ColorDistance(newBg, targetBackgroundColor) < 0.001f)
        {
            targetBlock.Color = targetBackgroundColor;
        }
        else
        {
            targetBlock.Color = newBg;
        }

        // Animate text color
        if (targetLabel != null)
        {
            Color currentText = targetLabel.Color;
            Color newText = Color.Lerp(currentText, targetTextColor, t);
            
            if (ColorDistance(newText, targetTextColor) < 0.001f)
            {
                targetLabel.Color = targetTextColor;
            }
            else
            {
                targetLabel.Color = newText;
            }
        }

        // Stop animating when close enough to target
        if (Vector3.Distance(targetBlock.transform.localScale, targetScale) < 0.001f &&
            ColorDistance(targetBlock.Color, targetBackgroundColor) < 0.001f &&
            (targetLabel == null || ColorDistance(targetLabel.Color, targetTextColor) < 0.001f))
        {
            isAnimating = false;
        }

        // Handle hover delay
        if (isHovered && !hoverDelayElapsed && hoverDelay > 0)
        {
            hoverTimer += Time.deltaTime;
            if (hoverTimer >= hoverDelay)
            {
                hoverDelayElapsed = true;
                ApplyState(hoverState);
            }
        }
    }

    private float ColorDistance(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a);
    }

    public void OnHover()
    {
        if (!enableHoverEffects || isPressed || isDisabled) return;
        
        isHovered = true;
        hoverTimer = 0f;
        hoverDelayElapsed = false;

        if (hoverDelay <= 0)
        {
            hoverDelayElapsed = true;
            ApplyState(hoverState);
            PlaySound(hoverSound);
        }
    }

    public void OnUnhover()
    {
        if (!enableHoverEffects || isPressed || isDisabled) return;
        
        isHovered = false;
        hoverTimer = 0f;
        hoverDelayElapsed = false;
        RestoreToOriginal();
    }

    public void OnPress()
    {
        if (!enablePressedEffects || isDisabled) return;
        
        isPressed = true;
        ApplyState(pressedState);
        PlaySound(pressSound);
    }

    public void OnRelease(bool stillHovering)
    {
        if (!enablePressedEffects || isDisabled) return;
        
        isPressed = false;
        PlaySound(releaseSound);

        if (stillHovering && enableHoverEffects)
        {
            ApplyState(hoverState);
        }
        else
        {
            RestoreToOriginal();
        }
    }

    public void OnCancel()
    {
        isPressed = false;
        isHovered = false;
        hoverTimer = 0f;
        hoverDelayElapsed = false;
        RestoreToOriginal();
    }

    public void SetDisabled(bool disabled)
    {
        isDisabled = disabled;
        
        if (disabled)
        {
            isPressed = false;
            isHovered = false;
            
            if (enableDisabledState)
            {
                ApplyState(disabledState);
            }
        }
        else
        {
            RestoreToOriginal();
        }
    }

    private void RestoreToOriginal()
    {
        if (targetBlock == null) return;

        targetScale = originalState.scale;
        targetBackgroundColor = originalState.backgroundColor;
        targetTextColor = originalState.textColor;

        if (!useSmoothTransitions)
        {
            targetBlock.transform.localScale = originalState.scale;
            targetBlock.Color = originalState.backgroundColor;
            if (targetLabel != null)
            {
                targetLabel.Color = originalState.textColor;
            }
        }
        else
        {
            animationProgress = 0f;
            isAnimating = true;
        }

        // Restore ALL original properties immediately (non-animated)
        targetBlock.Border.Enabled = originalState.borderEnabled;
        if (originalState.borderEnabled)
        {
            targetBlock.Border.Color = originalState.borderColor;
            targetBlock.Border.Width = originalState.borderWidth;
        }
        
        targetBlock.Shadow.Enabled = originalState.shadowEnabled;
        if (originalState.shadowEnabled)
        {
            targetBlock.Shadow.Color = originalState.shadowColor;
            targetBlock.Shadow.Blur = originalState.shadowBlur;
            targetBlock.Shadow.Offset = originalState.shadowOffset;
        }
        
        targetBlock.CornerRadius = originalState.cornerRadius;
    }

    private void ApplyState(VisualStateConfig state)
    {
        if (targetBlock == null) return;

        animationProgress = 0f;

        // Scale
        if (state.changeScale)
        {
            targetScale = state.scale;
            if (!useSmoothTransitions)
            {
                targetBlock.transform.localScale = targetScale;
            }
        }

        // Background Color
        if (state.changeBackgroundColor)
        {
            targetBackgroundColor = state.backgroundColor;
            if (!useSmoothTransitions)
            {
                targetBlock.Color = targetBackgroundColor;
            }
        }

        // Text Color
        if (targetLabel != null && state.changeTextColor)
        {
            targetTextColor = state.textColor;
            if (!useSmoothTransitions)
            {
                targetLabel.Color = targetTextColor;
            }
        }

        // Border (immediate, non-animated)
        if (state.changeBorder)
        {
            targetBlock.Border.Enabled = state.showBorder;
            if (state.showBorder)
            {
                targetBlock.Border.Color = state.borderColor;
                targetBlock.Border.Width = state.borderWidth;
            }
        }

        // Shadow (immediate, non-animated)
        if (state.changeShadow)
        {
            targetBlock.Shadow.Enabled = state.showShadow;
            if (state.showShadow)
            {
                targetBlock.Shadow.Color = state.shadowColor;
                targetBlock.Shadow.Blur = state.shadowBlur;
                targetBlock.Shadow.Offset = new Length2(state.shadowOffset.x, state.shadowOffset.y);
            }
        }

        // Corner Radius (immediate, non-animated)
        if (state.changeCornerRadius)
        {
            targetBlock.CornerRadius = state.cornerRadius;
        }

        if (useSmoothTransitions)
        {
            isAnimating = true;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioVolume > 0)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, audioVolume);
        }
    }

    public void ResetToDefault()
    {
        isPressed = false;
        isHovered = false;
        isDisabled = false;
        hoverTimer = 0f;
        hoverDelayElapsed = false;
        RestoreToOriginal();
    }

    // Public getters for state
    public bool IsPressed => isPressed;
    public bool IsHovered => isHovered;
    public bool IsDisabled => isDisabled;
}

/// <summary>
/// Configuration for a single visual state
/// </summary>
[System.Serializable]
public class VisualStateConfig
{
    [VisualStateProperty("Scale")]
    public bool changeScale = false;
    [ConditionalHide("changeScale")]
    public Vector3 scale = Vector3.one;

    [Space(5)]
    [VisualStateProperty("Background Color")]
    public bool changeBackgroundColor = false;
    [ConditionalHide("changeBackgroundColor")]
    public Color backgroundColor = Color.white;

    [Space(5)]
    [VisualStateProperty("Text Color")]
    public bool changeTextColor = false;
    [ConditionalHide("changeTextColor")]
    public Color textColor = Color.black;

    [Space(5)]
    [VisualStateProperty("Border")]
    public bool changeBorder = false;
    [ConditionalHide("changeBorder")]
    public bool showBorder = false;
    [ConditionalHide("changeBorder")]
    public Color borderColor = Color.black;
    [ConditionalHide("changeBorder")]
    public float borderWidth = 2f;

    [Space(5)]
    [VisualStateProperty("Shadow")]
    public bool changeShadow = false;
    [ConditionalHide("changeShadow")]
    public bool showShadow = false;
    [ConditionalHide("changeShadow")]
    public Color shadowColor = new Color(0, 0, 0, 0.3f);
    [ConditionalHide("changeShadow")]
    public float shadowBlur = 4f;
    [ConditionalHide("changeShadow")]
    public Vector2 shadowOffset = new Vector2(0, 2);

    [Space(5)]
    [VisualStateProperty("Corner Radius")]
    public bool changeCornerRadius = false;
    [ConditionalHide("changeCornerRadius")]
    public Length cornerRadius = 0;
}

public class VisualStatePropertyAttribute : PropertyAttribute
{
    public string Label { get; private set; }
    public VisualStatePropertyAttribute(string label)
    {
        Label = label;
    }
}

public class ConditionalHideAttribute : PropertyAttribute
{
    public string ConditionalFieldName { get; private set; }
    public ConditionalHideAttribute(string conditionalFieldName)
    {
        ConditionalFieldName = conditionalFieldName;
    }
}