using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.PixelSensors;
using System;


public class PixelSensorSnapshot {
    public byte[] rgbFrameData;
    public byte[] depthFrame;
    public Pose cameraPose;
}

/// <summary> Manages the detection of markers using MagicLeap2's features. </summary>
public class ML2DetectionManager : DeviceSubsystemManager
{
    public GameObject testObject;

    private PixelSensorSnapshot _lastSnapshot;

    private ML2CameraSensorTest _rgbPixelSensor;
    private ML2DepthPixelSensor _depthPixelSensor;

    private Camera mainCamera;

    /// <summary> Prepares everything needed for marker detection. </summary>
    private void Start()
    {

        // 1. Finding both Pixel Sensor Managers - RGB and Depth
        _rgbPixelSensor = GetComponent<ML2CameraSensorTest>();
        _depthPixelSensor = GetComponent<ML2DepthPixelSensor>();

        // 2. Subscribing to detection updates
        Detector.Instance.OnDetectionReceived += HandleDetectionResults;
        mainCamera = FindObjectOfType<Camera>();

        // 3. Setting handled subsystem type
        this._managedSubsystemType = SubsystemType.MarkerDetection;
    }

    private void OnDestroy()
    {
        // Unsubscribe from detection events
        Detector.Instance.OnDetectionReceived -= HandleDetectionResults;
    }


    /// <summary> Updates the marker detector and processes all (if any) detected markers. </summary>
    protected override void HandleSubsystem()
    {
        if (NetworkObjectManager.Instance != null && Detector.Instance.IsAvailable())
        {
            PixelSensorSnapshot snapshot = TryMakeSnapshot();
            if (snapshot != null)
            {
                _lastSnapshot = snapshot;

                Detector.Instance.SendMessageAsync(new DetectionRequest
                {
                    frameData = Convert.ToBase64String(snapshot.rgbFrameData)
                });
            }
        }
    }

    private PixelSensorSnapshot TryMakeSnapshot()
    {
        try
        {
            PixelSensorSnapshot snapshot = new PixelSensorSnapshot
            {
                rgbFrameData = _rgbPixelSensor.GetLastFrame(),
                depthFrame = _depthPixelSensor.GetLastFrame(),
                cameraPose = new Pose(mainCamera.transform.position, mainCamera.transform.rotation)
            };

            return snapshot;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Snapshot build failed: {ex.Message}");
            return null;
        }
    }


    // Called on event
    /// <summary> Processes each detected marker, verifies if its a valid detection, 
    ///           and triggers the event for valid detections.</summary>
    private void HandleDetectionResults(object sender, Detector.DetectionEventArgs e)
    {
        var response = e.response;

        if (response?.detections == null || response.detections.Count == 0)
        {
            Debug.Log($"[DetectionHandler] No detections received");
            return;
        }
        else
        {
            Debug.Log($"[DetectionHandler] Received {response.detections.Count} detection(s)");

            foreach (Detection detection in response.detections) {
                HandleDetectedMarker(detection);
            }        
        }
    }

    private Vector3 DetectionPoint2World(Vector2 detectionPoint)
    {
        float depth = _depthPixelSensor.GetDepthAtPixel(_lastSnapshot.depthFrame, detectionPoint.x, detectionPoint.y);

        if (depth > 0) {
            int screenX = (int)(detectionPoint.x * Screen.width);
            int screenY = (int)(detectionPoint.y * Screen.height);

            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(screenX, screenY, depth)); // TODO
            return worldPoint;
        }

        return Vector3.zero;
    }

    private void HandleDetectedMarker(Detection detection)
    {
        Vector3 accumulatedWorldPosition = Vector3.zero;
        Vector3[] worldCorners = new Vector3[4];
        int validPoints = 0;

        for (int i = 0; i < 4; i++)
        {
            Vector2 detectionCoords = new Vector2(detection.corners[i][0], detection.corners[i][1]);
            Vector3 worldPos = DetectionPoint2World(detectionCoords);
            if (worldPos.z != 0) {
                accumulatedWorldPosition += worldPos;
                worldCorners[i] = worldPos;
                validPoints++;
            }
        }

        if (validPoints == 4)
        {
            Vector3 markerCenter = accumulatedWorldPosition / validPoints;

            Vector3 side1 = worldCorners[1] - worldCorners[0];
            Vector3 side2 = worldCorners[2] - worldCorners[1];
            Vector3 sideVector = (side1.magnitude > side2.magnitude ? side1 : side2).normalized;

            //Quaternion markerRotation = Quaternion.LookRotation(sideVector);
            Quaternion markerRotation = Quaternion.Euler(0, 0, 0);

            // testObject.transform.position = markerCenter;
            // testObject.transform.rotation = Quaternion.LookRotation(markerRotation);
            NetworkObjectManager.Instance.ProcessMarkerServerRpc(
                 new MarkerInfo {
                    Id = detection.class_id,
                    Pose = new Pose(markerCenter, markerRotation),
                    Size = 0.035f
                 }
            );
        }
    }
}