using UnityEngine;

public class OpenCloseTest : MonoBehaviour
{
    [Header("Open/Close Settings")]
    public float spacing = 0.2f;       // Vertical spacing between children when open
    public float animationSpeed = 3f;  // How fast the movement happens
    public bool isOpen = false;        // Current state

    private Vector3[] closedPositions;
    private Vector3[] openPositions;

    void Start()
    {
        int count = transform.childCount;
        closedPositions = new Vector3[count];
        openPositions = new Vector3[count];

        // Record initial (closed) positions and compute open ones
        for (int i = 0; i < count; i++)
        {
            var child = transform.GetChild(i);
            closedPositions[i] = child.localPosition;
            openPositions[i] = new Vector3(
                closedPositions[i].x,
                i * spacing,
                closedPositions[i].z
            );
        }
    }

    void Update()
    {
        // Animate smoothly toward target positions
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            Vector3 target = isOpen ? openPositions[i] : closedPositions[i];
            child.localPosition = Vector3.Lerp(
                child.localPosition,
                target,
                Time.deltaTime * animationSpeed
            );
        }
    }

    // You can call this from a button, event, or key press
    [ContextMenu("Toggle Open/Close")]
    public void Toggle()
    {
        isOpen = !isOpen;
    }
}
