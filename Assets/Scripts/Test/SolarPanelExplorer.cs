using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Nova;

public class SolarPanelExplorer : MonoBehaviour
{
    [System.Serializable]
    public class LayerInfo
    {
        public string layerName;
        [TextArea(3, 10)]
        public string description;
    }

    [Header("Layer Configuration")]
    public List<LayerInfo> layerDescriptions = new List<LayerInfo>();
    
    [Header("Visual Settings")]
    public float expandedSpacing = 0.3f;
    public float highlightRotation = 15f;
    public float highlightScale = 1.1f;
    public float highlightXOffset = 0.5f; // X-axis offset for highlighted layer
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(1f, 0.8f, 0.3f);
    
    [Header("Animation Settings")]
    public float expandDuration = 0.8f;
    public float highlightDuration = 0.5f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("UI Settings")]
    public GameObject infoCanvas;
    public TextBlock  layerNameText;
    public TextBlock  descriptionText;
    public UnityEngine.UI.Button nextButton;
    public UnityEngine.UI.Button prevButton;
    public UnityEngine.UI.Button closeButton;
    
    private Vector3[] closedPositions;
    private Vector3[] expandedPositions;
    private Quaternion[] originalRotations;
    private Vector3[] originalScales;
    
    private int currentLayerIndex = -1;
    private bool isExpanded = false;
    private bool isAnimating = false;
    
    private List<Renderer> layerRenderers = new List<Renderer>();
    private List<MaterialPropertyBlock> propertyBlocks = new List<MaterialPropertyBlock>();

    void Start()
    {
        InitializeArrays();
        SetupUI();
        
        if (infoCanvas != null)
            infoCanvas.SetActive(false);
    }

    void InitializeArrays()
    {
        int count = transform.childCount;
        closedPositions = new Vector3[count];
        expandedPositions = new Vector3[count];
        originalRotations = new Quaternion[count];
        originalScales = new Vector3[count];
        
        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            closedPositions[i] = child.localPosition;
            expandedPositions[i] = new Vector3(
                closedPositions[i].x,
                i * expandedSpacing,
                closedPositions[i].z
            );
            originalRotations[i] = child.localRotation;
            originalScales[i] = child.localScale;
            
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                layerRenderers.Add(renderer);
                propertyBlocks.Add(new MaterialPropertyBlock());
            }
            else
            {
                layerRenderers.Add(null);
                propertyBlocks.Add(null);
            }
        }
        
        // Auto-populate layer descriptions if empty
        if (layerDescriptions.Count == 0)
        {
            for (int i = 0; i < count; i++)
            {
                layerDescriptions.Add(new LayerInfo
                {
                    layerName = $"Layer {i + 1}: {transform.GetChild(i).name}",
                    description = "Add description here..."
                });
            }
        }
    }

    void SetupUI()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(NextLayer);
        
        if (prevButton != null)
            prevButton.onClick.AddListener(PreviousLayer);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseExplorer);
    }

    void Update()
    {
        // Keyboard controls
        if (isExpanded && !isAnimating)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                NextLayer();
            
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                PreviousLayer();
            
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseExplorer();
        }
        
        // Toggle explorer with spacebar
        if (Input.GetKeyDown(KeyCode.Space) && !isAnimating)
        {
            if (!isExpanded)
                StartExplorer();
            else
                CloseExplorer();
        }
        
        // Click detection on layers
        if (isExpanded && Input.GetMouseButtonDown(0))
        {
            DetectLayerClick();
        }
    }

    void DetectLayerClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (hit.transform == transform.GetChild(i))
                {
                    HighlightLayer(i);
                    break;
                }
            }
        }
    }

    [ContextMenu("Start Explorer")]
    public void StartExplorer()
    {
        if (isAnimating) return;
        StartCoroutine(ExpandLayers());
    }

    [ContextMenu("Close Explorer")]
    public void CloseExplorer()
    {
        if (isAnimating) return;
        StartCoroutine(CollapseLayers());
    }

    public void NextLayer()
    {
        if (isAnimating) return;
        int nextIndex = (currentLayerIndex + 1) % transform.childCount;
        HighlightLayer(nextIndex);
    }

    public void PreviousLayer()
    {
        if (isAnimating) return;
        int prevIndex = currentLayerIndex - 1;
        if (prevIndex < 0) prevIndex = transform.childCount - 1;
        HighlightLayer(prevIndex);
    }

    public void HighlightLayer(int index)
    {
        if (isAnimating || !isExpanded) return;
        if (index < 0 || index >= transform.childCount) return;
        
        StartCoroutine(HighlightLayerCoroutine(index));
    }

    IEnumerator ExpandLayers()
    {
        isAnimating = true;
        isExpanded = true;
        
        float elapsed = 0f;
        
        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = easeCurve.Evaluate(elapsed / expandDuration);
            
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                child.localPosition = Vector3.Lerp(closedPositions[i], expandedPositions[i], t);
            }
            
            yield return null;
        }
        
        // Set final positions
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).localPosition = expandedPositions[i];
        }
        
        isAnimating = false;
        
        // Show UI and highlight first layer
        if (infoCanvas != null)
            infoCanvas.SetActive(true);
        
        HighlightLayer(0);
    }

    IEnumerator CollapseLayers()
    {
        isAnimating = true;
        
        // First, reset any highlighted layer
        if (currentLayerIndex >= 0)
        {
            yield return StartCoroutine(UnhighlightCurrentLayer());
        }
        
        if (infoCanvas != null)
            infoCanvas.SetActive(false);
        
        float elapsed = 0f;
        
        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = easeCurve.Evaluate(elapsed / expandDuration);
            
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                child.localPosition = Vector3.Lerp(expandedPositions[i], closedPositions[i], t);
            }
            
            yield return null;
        }
        
        // Set final positions
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).localPosition = closedPositions[i];
        }
        
        isExpanded = false;
        isAnimating = false;
        currentLayerIndex = -1;
    }

    IEnumerator HighlightLayerCoroutine(int index)
    {
        isAnimating = true;
        
        // Unhighlight previous layer
        if (currentLayerIndex >= 0 && currentLayerIndex != index)
        {
            yield return StartCoroutine(UnhighlightLayer(currentLayerIndex));
        }
        
        currentLayerIndex = index;
        Transform layer = transform.GetChild(index);
        
        // Update UI text
        UpdateUI(index);
        
        // Animate highlight
        float elapsed = 0f;
        Quaternion targetRotation = originalRotations[index] * Quaternion.Euler(0, highlightRotation, 0);
        Vector3 targetScale = originalScales[index] * highlightScale;
        
        // Add X offset to the expanded position
        Vector3 startPosition = expandedPositions[index];
        Vector3 targetPosition = new Vector3(
            expandedPositions[index].x + highlightXOffset,
            expandedPositions[index].y,
            expandedPositions[index].z
        );
        
        while (elapsed < highlightDuration)
        {
            elapsed += Time.deltaTime;
            float t = easeCurve.Evaluate(elapsed / highlightDuration);
            
            layer.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            layer.localRotation = Quaternion.Slerp(originalRotations[index], targetRotation, t);
            layer.localScale = Vector3.Lerp(originalScales[index], targetScale, t);
            
            // Color lerp
            if (layerRenderers[index] != null)
            {
                // Color c = Color.Lerp(normalColor, highlightColor, t);
                // propertyBlocks[index].SetColor("_Color", c);
                // layerRenderers[index].SetPropertyBlock(propertyBlocks[index]);
            }
            
            yield return null;
        }
        
        isAnimating = false;
    }

    IEnumerator UnhighlightLayer(int index)
    {
        Transform layer = transform.GetChild(index);
        Quaternion startRotation = layer.localRotation;
        Vector3 startScale = layer.localScale;
        Vector3 startPosition = layer.localPosition;
        
        float elapsed = 0f;
        
        while (elapsed < highlightDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (highlightDuration * 0.5f);
            
            layer.localPosition = Vector3.Lerp(startPosition, expandedPositions[index], t);
            layer.localRotation = Quaternion.Slerp(startRotation, originalRotations[index], t);
            layer.localScale = Vector3.Lerp(startScale, originalScales[index], t);
            
            if (layerRenderers[index] != null)
            {
                // Color c = Color.Lerp(highlightColor, normalColor, t);
                // propertyBlocks[index].SetColor("_Color", c);
                // layerRenderers[index].SetPropertyBlock(propertyBlocks[index]);
            }
            
            yield return null;
        }
        
        layer.localPosition = expandedPositions[index];
        layer.localRotation = originalRotations[index];
        layer.localScale = originalScales[index];
    }

    IEnumerator UnhighlightCurrentLayer()
    {
        if (currentLayerIndex >= 0)
        {
            yield return StartCoroutine(UnhighlightLayer(currentLayerIndex));
        }
    }

    void UpdateUI(int index)
    {
        if (index < 0 || index >= layerDescriptions.Count) return;
        
        if (layerNameText != null)
            layerNameText.Text = layerDescriptions[index].layerName;
        
        if (descriptionText != null)
            descriptionText.Text = layerDescriptions[index].description;
    }
}