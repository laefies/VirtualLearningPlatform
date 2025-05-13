using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Simulates XR interaction for desktop using raycasts to grab objects and interact with UI.
/// </summary>
public class SimulatedXRInteraction : XRBaseInteractor
{
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float rayDistance = 100f;
    [SerializeField] private Color rayColor = Color.white;
    [SerializeField] private Color rayColorHit = Color.green;
    [SerializeField] private Color rayColorSelected = Color.blue;
    [SerializeField] private float scrollSpeed = 0.25f;

    private XRGrabInteractable grabbed;
    private LineRenderer lineRenderer;
    private float currentGrabDistance;
    private bool isHitting = false;
    private bool isHittingInteractable = false;

    private Button hoveredUIButton;

    private void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.0005f;
        lineRenderer.endWidth = 0.0005f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 2;
    }

    void Update()
    {
        Ray ray = new Ray(rayOrigin.position, -rayOrigin.forward);
        isHitting = Physics.Raycast(ray, out RaycastHit hit, rayDistance);
        isHittingInteractable = false;
        hoveredUIButton = null;

        if (isHitting)
        {
            // Check for UI button
            hoveredUIButton = hit.collider.GetComponent<Button>();
            if (hoveredUIButton != null)
            {
                isHittingInteractable = true;
            }
            else
            {
                // Check for XR grab interactable
                var grabInteractable = hit.collider.GetComponent<XRGrabInteractable>();
                if (grabInteractable != null)
                {
                    isHittingInteractable = true;
                }
            }
        }

        float hitDistance = isHitting ? hit.distance : rayDistance;

        UpdateLineRenderer(ray, hitDistance);

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (hoveredUIButton != null)
            {
                hoveredUIButton.onClick.Invoke();
            }
            else if (isHitting)
            {
                var interactable = hit.collider.GetComponent<XRGrabInteractable>();
                if (interactable)
                {
                    grabbed = interactable;
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

    private void UpdateLineRenderer(Ray ray, float distance)
    {
        if (!lineRenderer) return;

        Color color;
        if (grabbed != null)
        {
            color = rayColorSelected;
        }
        else if (hoveredUIButton != null)
        {
            color = rayColorHit;
        }
        else if (isHittingInteractable)
        {
            color = rayColorHit;
        }
        else
        {
            color = rayColor;
        }

        lineRenderer.SetPosition(0, ray.origin);
        lineRenderer.SetPosition(1, ray.origin + ray.direction * distance);
        lineRenderer.startColor = color;
        lineRenderer.endColor = new Color(color.r, color.g, color.b, 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!rayOrigin) return;

        Ray ray = new Ray(rayOrigin.position, -rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            Gizmos.color = hit.collider.GetComponent<XRGrabInteractable>() ? rayColorHit : rayColor;
            Gizmos.DrawRay(ray.origin, ray.direction * hit.distance);
            Gizmos.DrawWireSphere(hit.point, 0.01f);
        }
        else
        {
            Gizmos.color = rayColor;
            Gizmos.DrawRay(ray.origin, ray.direction * rayDistance);
        }
    }

    protected override void OnSelectEntering(SelectEnterEventArgs args) { }

    protected override void OnSelectExiting(SelectExitEventArgs args) { }
}
