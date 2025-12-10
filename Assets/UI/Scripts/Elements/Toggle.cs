using Nova;
using UnityEngine;
using UnityEngine.Events;
using NovaSamples.UIControls;

/// <summary>
/// A UI control which reacts to user input and fires click events
/// </summary>
public class Toggle : UIControl<ToggleVisuals>
{
    [Tooltip("Event fired when the toggle is clicked.")]
    public UnityEvent OnClicked = null;

    [Tooltip("Event invoked when the toggle state changes. Provides the ToggledOn state.")]
    public UnityEvent<bool> OnToggled = null;

    [Tooltip("The toggle state of this toggle control")]
    [SerializeField]
    private bool toggledOn = false;

    /// <summary>
    /// The state of this toggle control
    /// </summary>
    public bool ToggledOn
    {
        get => toggledOn;
        set
        {
            if (value == toggledOn) return;
            toggledOn = value;

            UpdateToggleIndicator();
            OnToggled?.Invoke(toggledOn);

            visualFeedback?.SetSelected(ToggledOn);
        }
    }

    private Nova.Interactable interactableComponent;
    private InteractionFeedback visualFeedback;

    private void Awake()
    {
        interactableComponent = gameObject.GetComponent<Nova.Interactable>();
        visualFeedback = GetComponent<InteractionFeedback>();
    }

    public bool interactable
    {
        get => interactableComponent != null && interactableComponent.enabled;
        set
        {
            if (interactableComponent != null)
            {
                interactableComponent.enabled = value;
                visualFeedback?.SetDisabled(!value);
            }
        }
    }

    private void OnEnable()
    {
        if (View.TryGetVisuals(out ToggleVisuals visuals))
        {
            visuals.UpdateVisualState(VisualState.Default);
        }

        View.UIBlock.AddGestureHandler<Gesture.OnClick, ToggleVisuals>(HandleClicked);
        View.UIBlock.AddGestureHandler<Gesture.OnHover, ToggleVisuals>(HandleHovered);
        View.UIBlock.AddGestureHandler<Gesture.OnUnhover, ToggleVisuals>(HandleUnhovered);
        View.UIBlock.AddGestureHandler<Gesture.OnPress, ToggleVisuals>(HandlePressed);
        View.UIBlock.AddGestureHandler<Gesture.OnRelease, ToggleVisuals>(HandleReleased);
        View.UIBlock.AddGestureHandler<Gesture.OnCancel, ToggleVisuals>(HandlePressCanceled);

        UpdateToggleIndicator();
    }

    private void OnDisable()
    {
        View.UIBlock.RemoveGestureHandler<Gesture.OnClick, ToggleVisuals>(HandleClicked);
        View.UIBlock.RemoveGestureHandler<Gesture.OnHover, ToggleVisuals>(HandleHovered);
        View.UIBlock.RemoveGestureHandler<Gesture.OnUnhover, ToggleVisuals>(HandleUnhovered);
        View.UIBlock.RemoveGestureHandler<Gesture.OnPress, ToggleVisuals>(HandlePressed);
        View.UIBlock.RemoveGestureHandler<Gesture.OnRelease, ToggleVisuals>(HandleReleased);
        View.UIBlock.RemoveGestureHandler<Gesture.OnCancel, ToggleVisuals>(HandlePressCanceled);
    }

    public void AddListener(UnityAction action)
    {
        OnClicked?.AddListener(action);
    }

    public void RemoveListener(UnityAction action)
    {
        OnClicked?.RemoveListener(action);
    }

    public void RemoveAllListeners()
    {
        OnClicked?.RemoveAllListeners();
    }

    /// <summary>
    /// Flip the toggle state on click.
    /// </summary>
    /// <param name="evt">The click event data.</param>
    /// <param name="visuals">The toggle visuals associated with the click event.</param>
    private void HandleClicked(Gesture.OnClick evt, ToggleVisuals visuals) {
        ToggledOn = !ToggledOn;
        OnClicked?.Invoke();
    }

    /// <summary>
    /// Update the visual toggle indicate to match the underlying <see cref="ToggledOn"/> state.
    /// </summary>
    private void UpdateToggleIndicator()
    {
        if (!(View.Visuals is ToggleVisuals visuals) || visuals.IsOnIndicator == null)
        {
            return;
        }

        visuals.IsOnIndicator.gameObject.SetActive(toggledOn);
    }

    private void HandleHovered(Gesture.OnHover evt, ToggleVisuals visuals)
    {
        ToggleVisuals.HandleHovered(evt, visuals);
        visualFeedback?.OnHover();
    }

    private void HandleUnhovered(Gesture.OnUnhover evt, ToggleVisuals visuals)
    {
        ToggleVisuals.HandleUnhovered(evt, visuals);
        visualFeedback?.OnUnhover();
    }

    private void HandlePressed(Gesture.OnPress evt, ToggleVisuals visuals)
    {
        ToggleVisuals.HandlePressed(evt, visuals);
        visualFeedback?.OnPress();
    }

    private void HandleReleased(Gesture.OnRelease evt, ToggleVisuals visuals)
    {
        ToggleVisuals.HandleReleased(evt, visuals);
        visualFeedback?.OnRelease(evt.Hovering);
    }

    private void HandlePressCanceled(Gesture.OnCancel evt, ToggleVisuals visuals)
    {
        ToggleVisuals.HandlePressCanceled(evt, visuals);
        visualFeedback?.OnCancel();
    }

}