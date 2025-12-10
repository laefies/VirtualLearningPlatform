using Nova;
using UnityEngine;
using UnityEngine.Events;
using NovaSamples.UIControls;

/// <summary>
/// A UI control which reacts to user input and fires click events
/// </summary>
public class Button : UIControl<ButtonVisuals>
{
    [Tooltip("Event fired when the button is clicked.")]
    public UnityEvent OnClicked = null;

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
                if (value == interactableComponent.enabled) return;

                interactableComponent.enabled = value;
                visualFeedback?.SetDisabled(!value);
            }
        }
    }

    private void OnEnable()
    {
        if (View.TryGetVisuals(out ButtonVisuals visuals))
        {
            visuals.UpdateVisualState(VisualState.Default);
        }

        View.UIBlock.AddGestureHandler<Gesture.OnClick, ButtonVisuals>(HandleClicked);
        View.UIBlock.AddGestureHandler<Gesture.OnHover, ButtonVisuals>(HandleHovered);
        View.UIBlock.AddGestureHandler<Gesture.OnUnhover, ButtonVisuals>(HandleUnhovered);
        View.UIBlock.AddGestureHandler<Gesture.OnPress, ButtonVisuals>(HandlePressed);
        View.UIBlock.AddGestureHandler<Gesture.OnRelease, ButtonVisuals>(HandleReleased);
        View.UIBlock.AddGestureHandler<Gesture.OnCancel, ButtonVisuals>(HandlePressCanceled);
    }

    private void OnDisable()
    {
        View.UIBlock.RemoveGestureHandler<Gesture.OnClick, ButtonVisuals>(HandleClicked);
        View.UIBlock.RemoveGestureHandler<Gesture.OnHover, ButtonVisuals>(HandleHovered);
        View.UIBlock.RemoveGestureHandler<Gesture.OnUnhover, ButtonVisuals>(HandleUnhovered);
        View.UIBlock.RemoveGestureHandler<Gesture.OnPress, ButtonVisuals>(HandlePressed);
        View.UIBlock.RemoveGestureHandler<Gesture.OnRelease, ButtonVisuals>(HandleReleased);
        View.UIBlock.RemoveGestureHandler<Gesture.OnCancel, ButtonVisuals>(HandlePressCanceled);
    }

    public void AddListener(UnityAction action)
    {
        OnClicked.AddListener(action);
    }

    public void RemoveListener(UnityAction action)
    {
        OnClicked.RemoveListener(action);
    }

    public void RemoveAllListeners()
    {
        OnClicked.RemoveAllListeners();
    }

    private void HandleClicked(Gesture.OnClick evt, ButtonVisuals visuals) => OnClicked?.Invoke();

    // MODIFY THESE METHODS TO CALL visualFeedback:
    private void HandleHovered(Gesture.OnHover evt, ButtonVisuals visuals)
    {
        ButtonVisuals.HandleHovered(evt, visuals);
        visualFeedback?.OnHover(); // ADD THIS
    }

    private void HandleUnhovered(Gesture.OnUnhover evt, ButtonVisuals visuals)
    {
        ButtonVisuals.HandleUnhovered(evt, visuals);
        visualFeedback?.OnUnhover(); // ADD THIS
    }

    private void HandlePressed(Gesture.OnPress evt, ButtonVisuals visuals)
    {
        ButtonVisuals.HandlePressed(evt, visuals);
        visualFeedback?.OnPress(); // ADD THIS
    }

    private void HandleReleased(Gesture.OnRelease evt, ButtonVisuals visuals)
    {
        ButtonVisuals.HandleReleased(evt, visuals);
        visualFeedback?.OnRelease(evt.Hovering); // ADD THIS
    }

    private void HandlePressCanceled(Gesture.OnCancel evt, ButtonVisuals visuals)
    {
        ButtonVisuals.HandlePressCanceled(evt, visuals);
        visualFeedback?.OnCancel(); // ADD THIS
    }
}