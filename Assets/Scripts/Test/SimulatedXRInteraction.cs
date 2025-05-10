using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Simulates XR interaction capabilities for desktop development and testing.
/// Provides mouse-based object interaction with raycast-based selection and manipulation
/// without requiring physical XR controllers.
/// </summary>
public class SimulatedXRInteraction : XRBaseInteractor
{
    /// <summary>
    /// Transform that defines the origin and direction of the interaction ray.
    /// The proposed simulation uses a visualization of a controller.
    /// </summary>
    [SerializeField] private Transform rayOrigin;
    
    /// <summary> Maximum distance the interaction ray can travel. </summary>
    [SerializeField] private float rayDistance = 100f;
    
    /// <summary> Color of the interaction ray when not interacting with anything. </summary>
    [SerializeField] private Color rayColor = Color.white;
    
    /// <summary> Color of the interaction ray when pointing at a valid interactable object. </summary>
    [SerializeField] private Color rayColorHit = Color.green;

    /// <summary> Color of the interaction ray when actively selecting/grabbing an object. </summary>
    [SerializeField] private Color rayColorSelected = Color.blue;
    
    /// <summary>
    /// How quickly the grabbed object moves closer/further when scrolling the mouse wheel.
    /// </summary>
    [SerializeField] private float scrollSpeed = 0.25f;

    /// <summary> Reference to the currently grabbed object - if any. </summary>
    private XRGrabInteractable grabbed;
    
    /// <summary>  Visual representation of the interaction ray. </summary>
    private LineRenderer lineRenderer;
    
    /// <summary> Current distance of the grabbed object from the ray origin. </summary>
    private float currentGrabDistance;
    
    /// <summary> Whether the ray is currently hitting any collider. </summary>
    private bool isHitting = false;
    
    /// <summary> Whether the ray is hitting a valid interactable object. </summary>
    private bool isHittingInteractable = false;

    /// <summary> Initialize required components and configure the visual ray. </summary>
    private void Awake()
    {
        // Add and configure a LineRenderer component to visualize the interaction ray
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.001f;
        lineRenderer.endWidth   = 0.001f;
        lineRenderer.material   = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = rayColor;
        lineRenderer.endColor   = new Color(rayColor.r, rayColor.g, rayColor.b, 0.2f);
        lineRenderer.positionCount = 2; // Line has start and end points
    }

    /// <summary> Handle interaction raycast, object selection, and distance manipulation. </summary>
    void Update()
    {
        // Creating a ray pointing outward from the ray origin...
        Ray ray = new Ray(rayOrigin.position, -rayOrigin.forward);
        
        // ... and casting it to check for hits
        isHitting = Physics.Raycast(ray, out RaycastHit hit, rayDistance);
        isHittingInteractable = false;
        
        if (isHitting) {
            // In which cade the existance of a XRGrabInteractable component is also verified
            XRGrabInteractable hitInteractable = hit.collider.GetComponent<XRGrabInteractable>();
            isHittingInteractable = (hitInteractable != null);
        }
        
        // Either results in the distance from the origin to the object, or the maximum distance
        float hitDistance = isHitting ? hit.distance : rayDistance;
        
        // Update debug ray visualization
        Color debugRayColor;
        if (grabbed != null) {
            debugRayColor = rayColorSelected; // Currently grabbing an object
        } else if (isHittingInteractable) {
            debugRayColor = rayColorHit;      // Pointing at a valid interactable
        } else {
            debugRayColor = rayColor;         // Default state
        }
        Debug.DrawRay(ray.origin, ray.direction * hitDistance, debugRayColor);

        // Update LineRenderer position and color
        UpdateLineRenderer(ray, hit, hitDistance);

        // Handle grab initiation with E key
        if (Input.GetKeyDown(KeyCode.E)) {
            if (isHitting) {
                grabbed = hit.collider.GetComponent<XRGrabInteractable>();
                if (grabbed) {
                    // Record the initial grab distance and set up grab position
                    currentGrabDistance = hit.distance;
                    attachTransform.position = hit.point;
                    
                    // Notify the XR Interaction system of the selection
                    grabbed.interactionManager.SelectEnter(this, grabbed);
                }
            }
        }

        // Handle object distance manipulation with scroll wheel
        if (grabbed != null)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0)
            {
                // Adjust grab distance based on scroll input
                currentGrabDistance -= scrollInput * scrollSpeed;
                currentGrabDistance = Mathf.Clamp(currentGrabDistance, 0.1f, rayDistance);
                
                // Update the attach point position
                attachTransform.position = ray.origin + ray.direction * currentGrabDistance;
            }
        }

        // Handle releasing the grabbed object when E key is released
        if (Input.GetKeyUp(KeyCode.E) && grabbed)
        {
            grabbed.interactionManager.SelectExit(this, grabbed);
            grabbed = null;
        }
    }

    /// <summary>Updates the LineRenderer component to visualize the interaction ray ingame.</summary>
    /// <param name="ray">The ray being cast.</param>
    /// <param name="hit">Information about the raycast hit, if any.</param>
    /// <param name="hitDistance">Distance to the hit point or maximum ray distance.</param>
    private void UpdateLineRenderer(Ray ray, RaycastHit hit, float hitDistance)
    {
        if (lineRenderer != null)
        {
            // Set the start position at the ray origin
            lineRenderer.SetPosition(0, ray.origin);
            
            // Set the end position at the hit point or maximum ray distance
            if (isHitting) {
                lineRenderer.SetPosition(1, ray.origin + ray.direction * hit.distance);
            } else {
                lineRenderer.SetPosition(1, ray.origin + ray.direction * rayDistance);
            }
            
            // Determine the ray color based on interaction state
            Color currentColor;
            if (grabbed != null) {
                currentColor = rayColorSelected; // Currently grabbing an object
            } else if (isHittingInteractable) {
                currentColor = rayColorHit;      // Pointing at a valid interactable
            } else {
                currentColor = rayColor;         // Default state
            }
            
            // Apply colors to the line renderer
            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0.2f); // Fade out effect
        }
    }

    /// <summary> Draws debug gizmos in the Unity editor to help visualize the interaction ray. </summary>
    private void OnDrawGizmosSelected()
    {
        if (rayOrigin != null)
        {
            Ray ray = new Ray(rayOrigin.position, -rayOrigin.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance)) {
                XRGrabInteractable hitInteractable = hit.collider.GetComponent<XRGrabInteractable>();
                Gizmos.color = hitInteractable != null ? rayColorHit : rayColor;                
                Gizmos.DrawRay(ray.origin, ray.direction * hit.distance);                
                Gizmos.DrawWireSphere(hit.point, 0.01f);
            } else {
                Gizmos.color = rayColor;
                Gizmos.DrawRay(ray.origin, ray.direction * rayDistance);
            }
        }
    }

    /// <summary> Override of the base XRBaseInteractor method. </summary>
    protected override void OnSelectEntering(SelectEnterEventArgs args) { }
    
    /// <summary> Override of the base XRBaseInteractor method. </summary>
    protected override void OnSelectExiting(SelectExitEventArgs args) { }
}