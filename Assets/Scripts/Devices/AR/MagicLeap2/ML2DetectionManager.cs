using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.MarkerUnderstanding;
using System;

/// <summary> Manages the detection of markers using MagicLeap2's features. </summary>
public class ML2DetectionManager : MonoBehaviour
{
    /// <summary> Reference to the original transform point, needed for coordinate space conversion. </summary>
    private Transform _origin;

    /// <summary> Reference to MagicLeap's detection feature. </summary>
    private MagicLeapMarkerUnderstandingFeature _markerFeature;

    /// <summary> Detector used for marker tracking. </summary>
    private MarkerDetector _markerDetector;

    /// <summary> Prepares everything needed for marker detection. </summary>
    private void Start()
    {
        // 1. Verifying correct XR implementation - if the origin exists and if marker detection is available
        XROrigin xrOrigin = FindAnyObjectByType<XROrigin>();
        _markerFeature    = OpenXRSettings.Instance.GetFeature<MagicLeapMarkerUnderstandingFeature>();

        if (xrOrigin == null || _markerFeature == null || !_markerFeature.enabled)
        {
            Debug.LogError("Required XR components are missing. Disabling marker detection.");
            enabled = false;
            return;
        }

        // 2. Configuration of the detection settings and creating of the detector object
        var detectorSettings = new MarkerDetectorSettings
        {
            MarkerType = MarkerType.Aruco,
            ArucoSettings = { 
                EstimateArucoLength = true, 
                ArucoType = ArucoType.Dictionary_5x5_50 
            }
        };
        _markerDetector = _markerFeature.CreateMarkerDetector(detectorSettings);
        
        // 3. Saving device origin coordinates
        _origin = xrOrigin.CameraFloorOffsetObject.transform;
    }

    /// <summary> Updates the marker detector and processes all (if any) detected markers. </summary>
    private void Update()
    {
        _markerFeature.UpdateMarkerDetectors();
        if (_markerDetector.Status == MagicLeap.OpenXR.Features.MarkerUnderstanding.MarkerDetectorStatus.Ready)
        {
            ProcessMarkers();
        }
    }

    /// <summary> Processes each detected marker, verifies if its a valid detection, 
    ///           and triggers the event for valid detections.</summary>
    private void ProcessMarkers()
    {
        foreach (var markerData in _markerDetector.Data)
        {
            if (!markerData.MarkerPose.HasValue) continue;

            NetworkObjectManager.Instance.ProcessMarkerServerRpc(
                new MarkerInfo {
                    Id   = markerData.MarkerNumber.ToString(),
                    Pose = new Pose(_origin.TransformPoint(markerData.MarkerPose.Value.position),
                                    _origin.rotation * markerData.MarkerPose.Value.rotation),
                    Size = markerData.MarkerLength
                }
            );            
        }
    }
}