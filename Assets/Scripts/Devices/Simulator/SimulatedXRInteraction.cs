using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Simulates XR interaction for desktop using raycasts with a state machine pattern.
/// </summary>
public class SimulatedXRInteraction : XRBaseInteractor
{
    // State machine
    private enum InteractionState { Idle, HoveringUI, HoveringObject, GrabbingObject, DraggingSlider }
    private InteractionState currentState = InteractionState.Idle;

    // Serialized fields
    [SerializeField] private Color rayColor = Color.white;
    [SerializeField] private Color rayColorHit = Color.green;
    [SerializeField] private Color rayColorSelected = Color.blue;
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float rayDistance = 100f;
    [SerializeField] private float scrollSpeed = 0.25f;

    // State data
    private XRGrabInteractable grabbedObject;
    private GameObject hoveredUIElement;
    private Slider activeSlider;
    private XRGrabInteractable hoveredInteractable;
    private float currentGrabDistance;
    private Vector3 currentHitPoint;
    private float currentHitDistance;
    private Vector3 startingMousePosition;

    // Components
    private LineRenderer lineRenderer;
    private EventSystem eventSystem;
    private PointerEventData pointerEventData;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        InitializeLineRenderer();
        InitializeEventSystem();
    }

    private void InitializeLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.0005f;
        lineRenderer.endWidth = 0.0005f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 2;
    }

    private void InitializeEventSystem()
    {
        eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
        pointerEventData = new PointerEventData(eventSystem);
    }

    void Update()
    {
        // Create raycast
        RaycastHit physicsHit;
        Ray ray = new Ray(rayOrigin.position, -rayOrigin.forward);
        bool hitPhysics = Physics.Raycast(ray, out physicsHit, rayDistance);
        RaycastResult? uiHit = PerformUIRaycast(ray);

        // Determine if the ray hit any targets
        DetermineHitTarget(hitPhysics, physicsHit, uiHit);

        // Update state machine
        UpdateStateMachine();

        // Handle input based on the current state
        HandleInput(ray);

        // Update visuals
        UpdateLineRenderer(ray);
    }

    private void DetermineHitTarget(bool hitPhysics, RaycastHit physicsHit, RaycastResult? uiHit)
    {
        hoveredUIElement = null;
        hoveredInteractable = null;
        currentHitDistance = rayDistance;
        currentHitPoint = Vector3.zero;

        // Check UI hit first
        if (uiHit.HasValue && uiHit.Value.gameObject != null)
        {
            currentHitDistance = uiHit.Value.distance;
            currentHitPoint = uiHit.Value.worldPosition;

            // Check if it's an interactable UI element or has a slider in parent
            GameObject hitObject = uiHit.Value.gameObject;
            Selectable selectable = hitObject.GetComponent<Selectable>();
            Slider slider = hitObject.GetComponentInParent<Slider>();
            
            if ((selectable != null && selectable.interactable) || slider != null)
            {
                // Use the slider's gameObject if found, otherwise use the hit object
                hoveredUIElement = slider != null ? slider.gameObject : hitObject;
            }
        }

        // Check if physics hit is closer
        if (hitPhysics && physicsHit.distance < currentHitDistance)
        {
            currentHitDistance = physicsHit.distance;
            currentHitPoint = physicsHit.point;
            hoveredUIElement = null; // Physics takes priority
            hoveredInteractable = physicsHit.collider.GetComponent<XRGrabInteractable>();
        }
    }

    private void UpdateStateMachine()
    {
        InteractionState nextState = currentState;

        switch (currentState)
        {
            case InteractionState.Idle:
                if (hoveredUIElement != null)
                    nextState = InteractionState.HoveringUI;
                else if (hoveredInteractable != null)
                    nextState = InteractionState.HoveringObject;
                break;

            case InteractionState.HoveringUI:
                if (hoveredUIElement == null)
                    nextState = hoveredInteractable != null ? InteractionState.HoveringObject : InteractionState.Idle;
                break;

            case InteractionState.HoveringObject:
                if (hoveredInteractable == null)
                    nextState = hoveredUIElement != null ? InteractionState.HoveringUI : InteractionState.Idle;
                break;

            case InteractionState.GrabbingObject:
                if (grabbedObject == null) {
                    nextState = InteractionState.Idle;
                    currentGrabDistance = currentHitDistance;
                }
                break;

            case InteractionState.DraggingSlider:
                if (activeSlider == null || Input.GetMouseButtonUp(0))
                    nextState = InteractionState.Idle;
                break;
        }

        currentState = nextState;
    }


    private void HandleInput(Ray ray)
    {
        switch (currentState)
        {
            case InteractionState.HoveringUI:
                HandleUIInput();
                break;

            case InteractionState.HoveringObject:
                HandleObjectInput(ray);
                break;

            case InteractionState.GrabbingObject:
                HandleGrabbingInput(ray);
                break;

            case InteractionState.DraggingSlider:
                HandleSliderInput();
                break;
        }
    }

    private void HandleUIInput() {
        if (Input.GetMouseButtonDown(0) && hoveredUIElement != null)
        {
            // Check if it's a slider
            Slider slider = hoveredUIElement.GetComponent<Slider>();
            if (slider != null)
            {
                activeSlider = slider;
                startingMousePosition = Input.mousePosition;
                currentState = InteractionState.DraggingSlider;
            }
            else
            {
                // Use ExecuteEvents to simulate a proper pointer click for other UI elements
                ExecuteEvents.Execute(hoveredUIElement, pointerEventData, ExecuteEvents.pointerClickHandler);
            }
        }
    }

    private void HandleObjectInput(Ray ray) {
        if (Input.GetMouseButtonDown(0) && hoveredInteractable != null)
        {
            grabbedObject = hoveredInteractable;
            attachTransform.position = currentHitPoint;
            grabbedObject.interactionManager.SelectEnter(this, grabbedObject);
            currentState = InteractionState.GrabbingObject;
        }
    }

    private void HandleGrabbingInput(Ray ray) {
        // Handle scroll to adjust distance
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            currentGrabDistance += scrollInput * scrollSpeed;
            currentGrabDistance = Mathf.Clamp(currentGrabDistance, 0.1f, rayDistance);
            attachTransform.position = ray.origin + ray.direction * currentGrabDistance;
        }

        // Handle release
        if (Input.GetMouseButtonUp(0))
        {
            if (grabbedObject != null)
            {
                grabbedObject.interactionManager.SelectExit(this, grabbedObject);
                grabbedObject = null;
            }
            currentState = InteractionState.Idle;
        }
    }

    private void HandleSliderInput()
    {
        if (activeSlider == null) return;

        float sensitivity = 0.5f;
        activeSlider.value += Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime * 60f;

        // Release on mouse up
        if (Input.GetMouseButtonUp(0))
        {
            activeSlider = null;
            currentState = InteractionState.Idle;
        }
    }

    private RaycastResult? PerformUIRaycast(Ray ray)
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        RaycastResult? closestHit = null;
        float closestDistance = float.MaxValue;

        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                RaycastResult? hit = RaycastWorldSpaceCanvas(canvas, ray);
                if (hit.HasValue && hit.Value.distance < closestDistance)
                {
                    closestDistance = hit.Value.distance;
                    closestHit = hit;
                }
            }
        }

        return closestHit;
    }

    private RaycastResult? RaycastWorldSpaceCanvas(Canvas canvas, Ray ray)
    {
        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null) return null;

        Camera cam = canvas.worldCamera ?? Camera.main;
        if (cam == null) return null;

        Plane canvasPlane = new Plane(-canvas.transform.forward, canvas.transform.position);
        if (canvasPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 screenPoint = cam.WorldToScreenPoint(hitPoint);
            pointerEventData.position = screenPoint;
            raycastResults.Clear();
            raycaster.Raycast(pointerEventData, raycastResults);

            if (raycastResults.Count > 0)
            {
                RaycastResult result = raycastResults[0];
                result.distance = enter;
                return result;
            }
        }

        return null;
    }

    private void UpdateLineRenderer(Ray ray)
    {
        if (!lineRenderer) return;

        Color color = GetRayColorForCurrentState();
        float distance = currentState == InteractionState.GrabbingObject ? currentGrabDistance : currentHitDistance;

        lineRenderer.SetPosition(0, ray.origin);
        lineRenderer.SetPosition(1, ray.origin + ray.direction * distance);
        lineRenderer.startColor = color;
        lineRenderer.endColor = new Color(color.r, color.g, color.b, 0.2f);
    }

    private Color GetRayColorForCurrentState()
    {
        switch (currentState)
        {
            case InteractionState.GrabbingObject:
            case InteractionState.DraggingSlider:
                return rayColorSelected;
            case InteractionState.HoveringUI:
            case InteractionState.HoveringObject:
                return rayColorHit;
            default:
                return rayColor;
        }
    }

    protected override void OnSelectEntering(SelectEnterEventArgs args) { }
    protected override void OnSelectExiting(SelectExitEventArgs args) { }
}