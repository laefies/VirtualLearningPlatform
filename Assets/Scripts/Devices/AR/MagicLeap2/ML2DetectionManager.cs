using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.MarkerUnderstanding;

public class ML2DetectionManager : MonoBehaviour
{
    private Transform _origin;
    private MagicLeapMarkerUnderstandingFeature _markerFeature;
    private MarkerDetector _markerDetector;
    private AlignmentManager _alignmentManager;

    private void Start()
    {
        XROrigin xrOrigin = FindAnyObjectByType<XROrigin>();
        _markerFeature = OpenXRSettings.Instance.GetFeature<MagicLeapMarkerUnderstandingFeature>();
        _alignmentManager = GetComponent<AlignmentManager>();

        if (xrOrigin == null || _markerFeature == null || !_markerFeature.enabled)
        {
            Debug.LogError("Required XR components are missing. Disabling marker detection.");
            enabled = false;
            return;
        }

        var detectorSettings = new MarkerDetectorSettings
        {
            MarkerType = MarkerType.Aruco,
            ArucoSettings = { 
                EstimateArucoLength = true, 
                ArucoType = ArucoType.Dictionary_5x5_50 
            }
        };

        _markerDetector = _markerFeature.CreateMarkerDetector(detectorSettings);
        _origin         = xrOrigin.CameraFloorOffsetObject.transform;
    }

    private void Update()
    {
        _markerFeature.UpdateMarkerDetectors();
        if (_markerDetector.Status == MagicLeap.OpenXR.Features.MarkerUnderstanding.MarkerDetectorStatus.Ready)
        {
            ProcessMarkers();
        }
    }

    private void ProcessMarkers()
    {

        foreach (var markerData in _markerDetector.Data)
        {
            if (!markerData.MarkerPose.HasValue) continue;

            var markerInfo = new MarkerInfo
            {
                Id = markerData.MarkerNumber.ToString(),
                Pose = new Pose(_origin.TransformPoint(markerData.MarkerPose.Value.position),
                                _origin.rotation * markerData.MarkerPose.Value.rotation),
                Size = markerData.MarkerLength
            };

            _alignmentManager.ProcessMarker(markerInfo);
        }
    }
}