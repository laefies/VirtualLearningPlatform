using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using MagicLeap.Android;
using MagicLeap.OpenXR.Features.Meshing;

public class MeshingTest : MonoBehaviour
{
    [SerializeField]
    private ARMeshManager meshManager;
    private MagicLeapMeshingFeature meshingFeature;

    private readonly MLPermissions.Callbacks _permissionCallbacks = new MLPermissions.Callbacks();

    private void OnValidate()
    {
        if (meshManager == null)
        {
            meshManager = FindObjectOfType<ARMeshManager>();
        }
    }

    private void Awake() {
        _permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        _permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        _permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
    }

    private void OnDestroy()
    {
        _permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        _permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        _permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
    }

    private IEnumerator Start()
    {
        if (meshManager == null)
        {
            Debug.LogError("No ARMeshManager component found. Disabling script.");
            enabled = false;
            yield break;
        }

        meshManager.enabled = false;

        // Add debug logging to see if we're reaching this point
        Debug.Log("Checking if meshing subsystem is loaded...");
        
        yield return new WaitUntil(IsMeshingSubsystemLoaded);
        Debug.Log("Meshing subsystem loaded successfully!");
        
        meshingFeature = OpenXRSettings.Instance.GetFeature<MagicLeapMeshingFeature>();
        if (!meshingFeature.enabled)
        {
            Debug.LogError("MagicLeapMeshingFeature was not enabled. Disabling script");
            enabled = false;
            yield break;
        }

        Debug.Log("About to request SpatialMapping permission...");
        
        // Try checking the current permission status before requesting
        bool hasPermission = MLPermissions.CheckPermission(MLPermission.SpatialMapping).IsOk;
        Debug.Log($"Current SpatialMapping permission status: {(hasPermission ? "Granted" : "Not Granted")}");
        
        if (!hasPermission)
        {
            MLPermissions.RequestPermission(MLPermission.SpatialMapping, _permissionCallbacks);
            Debug.Log("Permission request sent!");
        }
        else
        {
            Debug.Log("Permission already granted, enabling mesh manager");
            meshManager.enabled = true;
        }
    }

    private void OnPermissionGranted(string permission)
    {
        meshManager.enabled = true;
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Permission {MLPermission.SpatialMapping} denied. Disabling script.");
        enabled = false;
    }

    private bool IsMeshingSubsystemLoaded()
    {
        if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null) return false;
        var activeLoader = XRGeneralSettings.Instance.Manager.activeLoader;
        return activeLoader != null && activeLoader.GetLoadedSubsystem<XRMeshSubsystem>() != null;
    }
}