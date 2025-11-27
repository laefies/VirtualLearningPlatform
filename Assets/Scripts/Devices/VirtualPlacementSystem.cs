using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VirtualPlacementInputActionHandler))]
public class VirtualPlacementSystem : MonoBehaviour
{
    [SerializeField] private int PREVIEW_LAYER = 6;
    [SerializeField] private float MAX_STEP  = 0.02f;
    [SerializeField] private float LIFT_STEP = 0.001f;
    [SerializeField] private Material INVALID_MATERIAL;

    private bool canPlace;
    private Pose lastValidPose;
    private int objectIndex;
    private GameObject objectPreview;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    public static VirtualPlacementSystem Instance{ get; private set;}

    private void Awake() { Instance = this; }

    void Start() { objectIndex = -1; }

    public bool InitPlacement() {
        if (!NetworkObjectManager.Instance) return false;
            
        canPlace = false;
        objectIndex = -1;
        GetNextObject();
        
        return objectPreview != null;
    }

    public void StopPlacement() {
        if (objectPreview) {
            Destroy(objectPreview);
            objectPreview = null;
            canPlace = false;
        }
    }

    public bool ConfirmPlacement() {
        if (objectPreview && canPlace && TryPlaceObject()) {
            StopPlacement();
            return true;
        }
        return false;
    }

    void GetNextObject() {
        if (!NetworkObjectManager.Instance) return;

        // Verify if there are objects to spawn
        DetectionConfiguration config = NetworkObjectManager.Instance.config;
        if (config == null || config.GetObjectCount() == 0) {
            Debug.LogError("No objects to spawn!");
            return;
        }

        // Destroy previously previewed object (if theye exist)
        if (objectPreview) {
            Destroy(objectPreview);
            objectPreview = null;
        }

        // Increase index count and get next spawnable object
        objectIndex = (objectIndex + 1) % config.GetObjectCount();
        GameObject prefab = config.GetPrefab(objectIndex);

        // Validate object
        Spawnable spawnable = prefab.GetComponent<Spawnable>();
        if (spawnable == null || spawnable.vrProxy == null) {
            Debug.LogError($"Prefab '{prefab.name}' is missing Spawnable or Proxy component!");
            return;
        }

        // Instantiate the object's preview
        objectPreview = Instantiate(spawnable.vrProxy);
        objectPreview.transform.localScale = spawnable.vrProxy.transform.localScale * 0.05f;
        PreparePlacementPreview();
    }

    public void RotateBy(float yAngle)
    {
        objectPreview.transform.eulerAngles += new Vector3(0, yAngle, 0);
    }

    public void UpdatePreview(Vector3 pos)
    {
        if (objectPreview) objectPreview.transform.position = pos;

        float lifted = 0f;
        bool isValid = true;
        
        // If necessary, tweak position
        while (IsColliding()) {
            objectPreview.transform.position += Vector3.up * LIFT_STEP;
            lifted += LIFT_STEP;

            if (lifted >= MAX_STEP) {
                isValid = false;
                break;
            }
        }
        
        // Only update materials if validity state changed
        if (isValid != canPlace) {
            if (isValid) RestoreMaterials(); else ShowInvalidMaterial();
            canPlace = isValid;
        }

        if (isValid) lastValidPose = new Pose(objectPreview.transform.position, objectPreview.transform.rotation);

    }

    bool IsColliding()
    {
        Bounds b = objectPreview.GetComponent<Collider>().bounds;
        Collider[] hits = Physics.OverlapBox( objectPreview.transform.position, b.extents, objectPreview.transform.rotation, -1 );

        foreach (var h in hits) {
            if (h.transform != objectPreview.transform) return true;
        }
        return false;
    }
    
    private void ShowInvalidMaterial()
    {
        if (!objectPreview || INVALID_MATERIAL == null) return;
        
        Renderer[] renderers = objectPreview.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer rend in renderers) {
            Material[] mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = INVALID_MATERIAL;
            rend.materials = mats;
        }
    }

    private void PreparePlacementPreview()
    {
        if (!objectPreview) return;
        
        // Add RigidBody to detect collisions
        objectPreview.AddComponent<Rigidbody>();

        // Store original materials and prepare layer rendering for visual feedback
        originalMaterials.Clear();
        Renderer[] renderers = objectPreview.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer rend in renderers) {
            rend.gameObject.layer = PREVIEW_LAYER;
            originalMaterials[rend] = rend.materials;
        }
    }
    
    private void RestoreMaterials()
    {
        if (!objectPreview) return;
        
        foreach (var kvp in originalMaterials) {
            if (kvp.Key != null) kvp.Key.materials = kvp.Value;
        }
    }


    bool TryPlaceObject() 
    { 
        if (!NetworkObjectManager.Instance) return false;
        
        // Verify if there are objects to spawn
        DetectionConfiguration config = NetworkObjectManager.Instance.config;
        if (config == null || config.GetObjectCount() == 0) { 
            Debug.LogError("Object can not be spawned!"); 
            return false;
        } 
        
        try {
            NetworkObjectManager.Instance.ProcessMarkerServerRpc( 
                new MarkerInfo { 
                    Id = config.GetIdentificator(objectIndex), 
                    Pose = lastValidPose,
                    Size = 0.05f 
                } 
            );
            return true;
        } catch (System.Exception e) {
            Debug.LogError($"Something went wrong while spawning the object!");
            return false;
        }
    }
}