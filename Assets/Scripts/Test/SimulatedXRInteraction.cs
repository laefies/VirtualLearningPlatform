using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SimulatedXRInteraction : XRBaseInteractor
{
    [SerializeField] private Transform rayOrigin; 
    [SerializeField] private float rayDistance = 100f;
    [SerializeField] private Color rayColor = Color.white; 
    [SerializeField] private Color rayColorSelected = Color.blue;
    [SerializeField] private Color rayColorHit = Color.green; // New color for when ray hits something
    [SerializeField] private float scrollSpeed = 0.25f;

    private XRGrabInteractable grabbed;
    private LineRenderer lineRenderer; 
    private float currentGrabDistance;
    private bool isHitting = false;

    private void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.001f;
        lineRenderer.endWidth = 0.001f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.2f);
        lineRenderer.positionCount = 2;
    }

    void Update()
    {
        Ray ray = new Ray(rayOrigin.position, -rayOrigin.forward);        
        
        // Check if ray hits anything
        isHitting = Physics.Raycast(ray, out RaycastHit hit, rayDistance);
        float hitDistance = isHitting ? hit.distance : rayDistance;
        
        // Update debug ray visualization
        Debug.DrawRay(ray.origin, ray.direction * hitDistance, 
                    grabbed ? rayColorSelected : (isHitting ? rayColorHit : rayColor));

        // Update LineRenderer
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, ray.origin);
            
            // If hitting something, end the line at the hit point
            if (isHitting)
            {
                lineRenderer.SetPosition(1, ray.origin + ray.direction * hit.distance);
            }
            else
            {
                lineRenderer.SetPosition(1, ray.origin + ray.direction * rayDistance);
            }
            
            // Set colors based on state
            Color currentColor;
            if (grabbed != null)
            {
                currentColor = rayColorSelected;
            }
            else if (isHitting)
            {
                currentColor = rayColorHit;
            }
            else
            {
                currentColor = rayColor;
            }
            
            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0.2f);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isHitting)
            {
                grabbed = hit.collider.GetComponent<XRGrabInteractable>();
                if (grabbed)
                {
                    currentGrabDistance = hit.distance;                    
                    attachTransform.position = hit.point;
                    grabbed.interactionManager.SelectEnter(this, grabbed);
                }
            }
        }

        if (grabbed != null)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0)
            {
                currentGrabDistance -= scrollInput * scrollSpeed;
                currentGrabDistance = Mathf.Clamp(currentGrabDistance, 0.1f, rayDistance);
                
                attachTransform.position = ray.origin + ray.direction * currentGrabDistance;
            }
        }

        if (Input.GetKeyUp(KeyCode.E) && grabbed)
        {
            grabbed.interactionManager.SelectExit(this, grabbed);
            grabbed = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (rayOrigin != null)
        {
            Ray ray = new Ray(rayOrigin.position, -rayOrigin.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                // Draw a green ray to the hit point
                Gizmos.color = rayColorHit;
                Gizmos.DrawRay(ray.origin, ray.direction * hit.distance);
                
                // Draw a sphere at the hit point for better visibility
                Gizmos.DrawWireSphere(hit.point, 0.01f);
            }
            else
            {
                // No hit, draw the full ray in default color
                Gizmos.color = rayColor;
                Gizmos.DrawRay(ray.origin, ray.direction * rayDistance);
            }
        }
    }

    protected override void OnSelectEntering(SelectEnterEventArgs args) { }
    protected override void OnSelectExiting(SelectExitEventArgs args) { }
}