using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.MarkerUnderstanding;

public class ArUcoMarkerTesting : MonoBehaviour
{
    [Tooltip("Set the XR Origin so that the marker appears relative to headset's origin. If null, the script will try to find the component automatically.")]
    public XROrigin XROrigin;

    [Tooltip("If Not Null, this is the object that will be created at the position of each detected marker.")]
    public GameObject MarkerPrefab;

    public ArucoType ArucoType = ArucoType.Dictionary_5x5_50;

    public MarkerDetectorProfile DetectorProfile = MarkerDetectorProfile.Default;

    private MarkerDetectorSettings _detectorSettings;
    private MagicLeapMarkerUnderstandingFeature _markerFeature;
    private readonly Dictionary<string, GameObject> _markerObjectById = new Dictionary<string, GameObject>();

    private void OnValidate()
    {
        // Automatically find the XROrigin component if it's present in the scene
        if (XROrigin == null)
        {
            XROrigin = FindAnyObjectByType<XROrigin>();
        }
    }

    private void Start()
    {
        _markerFeature = OpenXRSettings.Instance.GetFeature<MagicLeapMarkerUnderstandingFeature>();

        if (_markerFeature == null || _markerFeature.enabled == false)
        {
            Debug.LogError("The Magic Leap 2 Marker Understanding OpenXR Feature is missing or disabled enabled. Disabling Script.");
            this.enabled = false;
            return;
        }

        if (XROrigin == null)
        {
            Debug.LogError("No XR Origin Found, markers sample will not work. Disabling Script.");
            this.enabled = false;
        }

        // Create the Marker Detector Settings
        _detectorSettings = new MarkerDetectorSettings();

        // Configure a generic detector with QR and Aruco Detector settings 
        _detectorSettings.QRSettings.EstimateQRLength = true;
        _detectorSettings.ArucoSettings.EstimateArucoLength = true;
        _detectorSettings.ArucoSettings.ArucoType = ArucoType;

        _detectorSettings.MarkerDetectorProfile = DetectorProfile;

        // We use the same settings on all 3 of the 
        // different detectors and target the specific marker by setting the Marker Type before creating the detector 

        // Create Aruco detector
        _detectorSettings.MarkerType = MarkerType.Aruco;
        _markerFeature.CreateMarkerDetector(_detectorSettings);

        // Create QRCode Detector
        _detectorSettings.MarkerType = MarkerType.QR;
        _markerFeature.CreateMarkerDetector(_detectorSettings);

        // Create UPCA Detector
        _detectorSettings.MarkerType = MarkerType.UPCA;
        _markerFeature.CreateMarkerDetector(_detectorSettings);
    }

    private void OnDestroy()
    {
        if (_markerFeature != null)
        {
            _markerFeature.DestroyAllMarkerDetectors();
        }
    }

    void Update()
    {
        // Update the marker detector
        _markerFeature.UpdateMarkerDetectors();

        // Iterate through all of the marker detectors
        for (int i = 0; i < _markerFeature.MarkerDetectors.Count; i++)
        {
            // Verify that the marker detector is running
            if (_markerFeature.MarkerDetectors[i].Status == MarkerDetectorStatus.Ready)
            {
                // Cycle through the detector's data and log it to the debug log
                MarkerDetector currentDetector = _markerFeature.MarkerDetectors[i];
                OnUpdateDetector(currentDetector);
            }
        }
    }

    private void OnUpdateDetector(MarkerDetector detector)
    {

        for (int i = 0; i < detector.Data.Count; i++)
        {
            string id = "";
            float markerSize = .01f;
            var data = detector.Data[i];
            switch (detector.Settings.MarkerType)
            {
                case MarkerType.Aruco:
                    id = data.MarkerNumber.ToString();
                    markerSize = data.MarkerLength;
                    break;
                case MarkerType.QR:
                    id = data.MarkerString;
                    markerSize = data.MarkerLength;
                    break;
                case MarkerType.UPCA:
                    Debug.Log("No pose is given for marker type UPCA, Code value is " + data.MarkerString);
                    break;
            }

            if (!data.MarkerPose.HasValue)
            {
                Debug.Log("Marker Pose not estimated yet.");
                return;
            }

            if (!string.IsNullOrEmpty(id) && markerSize > 0)
            {
                // If the marker ID has not been tracked create a new marker object
                if (!_markerObjectById.ContainsKey(id))
                {
                    // Create a primitive cube
                    if (MarkerPrefab)
                    {
                        GameObject newMarker = Instantiate(MarkerPrefab);
                        _markerObjectById.Add(id, newMarker);
                    }
                    else
                    {
                        GameObject newDefaultMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        _markerObjectById.Add(id, newDefaultMarker);
                    }

                }

                GameObject marker = _markerObjectById[id];
                SetTransformToMarkerPose(marker.transform, data.MarkerPose.Value, markerSize);
            }
        }
    }

    private void SetTransformToMarkerPose(Transform marker, Pose markerPose, float markerSize)
    {
        Transform originTransform = XROrigin.CameraFloorOffsetObject.transform;

        // Set the position of the marker. Since the pose is given relative to the XR Origin,
        // we need to transform it to world coordinates.
        marker.position = originTransform.TransformPoint(markerPose.position);
        marker.rotation = originTransform.rotation * markerPose.rotation;
        marker.localScale = new Vector3(markerSize, markerSize, markerSize);
    }
}