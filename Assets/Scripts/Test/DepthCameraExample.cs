using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MagicLeap.Android;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.OpenXR;
using System.Runtime.InteropServices;
using MagicLeap.OpenXR.Features.PixelSensors;

using System;

public class DepthCameraExample : MonoBehaviour
{

    [Header("General Configuration")]
    //public DepthStreamVisualizer streamVisualizer;

    [Tooltip("If Tue will return a raw depth image. If False will return depth32")]
    public bool UseRawDepth;
    private const string depthCameraSensorPath = "/pixelsensor/depth/center";

    public GameObject testObject;

    [Range(0.2f, 5.00f)] public float DepthRange;
    [Header("ShortRange =< 1m")] public ShortRangeUpdateRate SRUpdateRate;
    [Header("LongRange   > 1m")] public LongRangeUpdateRate  LRUpdateRate;

    public enum LongRangeUpdateRate  { OneFps = 1,  FiveFps = 5 }
    public enum ShortRangeUpdateRate { FiveFps = 5, ThirtyFps = 30, SixtyFps = 60 }


    private MagicLeapPixelSensorFeature pixelSensorFeature;
    private PixelSensorId? sensorId;
    private List<uint> configuredStreams = new List<uint>();

    public uint targetStream
    {
        get { return DepthRange > 1.0f ? (uint)0 : (uint)1; }
    }

    void Start()
    {
        pixelSensorFeature = OpenXRSettings.Instance.GetFeature<MagicLeapPixelSensorFeature>();
        if (pixelSensorFeature == null || !pixelSensorFeature.enabled)
        {
            Debug.LogError("Pixel Sensor Feature not found or not enabled!");
            enabled = false;
            return;
        }
        Permissions.RequestPermission(MLPermission.DepthCamera, OnPermissionGranted, OnPermissionDenied, OnPermissionDenied);
    }

    private void OnPermissionGranted(string permission)
    {
        if (permission.Contains(MLPermission.DepthCamera)) FindAndInitializeSensor();
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Permission { permission} not granted. Example script will not work.");
        enabled = false;
    }

    private void FindAndInitializeSensor()
    {
        var sensors = pixelSensorFeature.GetSupportedSensors();

        foreach (var sensor in sensors)
        {
            if (sensor.XrPathString.Contains(depthCameraSensorPath))
            {
                Debug.Log("[Depth Sensor] Sensor found!");
                sensorId = sensor;
                break;
            }
        }

        if (!sensorId.HasValue)
        {
            Debug.LogError("[Depth Sensor] Sensor not found...");
            return;
        }

        pixelSensorFeature.OnSensorAvailabilityChanged += OnSensorAvailabilityChanged;
        TryInitializeSensor();
    }

    private void OnSensorAvailabilityChanged(PixelSensorId id, bool available)
    {
        if (sensorId.HasValue && id == sensorId && available)
        {
            Debug.Log("[Depth Sensor] Sensor became available.");
            TryInitializeSensor();
        }
    }

    private void TryInitializeSensor()
    {
        if (sensorId.HasValue && pixelSensorFeature.GetSensorStatus(sensorId.Value) ==
            PixelSensorStatus.Undefined && pixelSensorFeature.CreatePixelSensor(sensorId.Value))
        {
            Debug.Log("[Depth Sensor] Sensor created successfully.");
            ConfigureSensorStreams();
        }
        else
        {
            Debug.LogWarning("[Depth Sensor] Failed to create sensor. Will retry when it becomes available.");
        }
    }

    // The capabilities that the script will edit
    private PixelSensorCapabilityType[] targetCapabilityTypes = new[]
    {
        PixelSensorCapabilityType.UpdateRate, PixelSensorCapabilityType.Format,
        PixelSensorCapabilityType.Resolution, PixelSensorCapabilityType.Depth,
    };


    private void ConfigureSensorStreams()
    {
        if (!sensorId.HasValue)
        {
            Debug.LogError("[Depth Sensor] Sensor ID not set.");
            return;
        }

        uint streamCount = pixelSensorFeature.GetStreamCount(sensorId.Value);
        if (streamCount < 1)
        {
            Debug.LogError("[Depth Sensor] Expected at least one stream from the sensor.");
            return;
        }

        configuredStreams.Add(targetStream);

        pixelSensorFeature.GetPixelSensorCapabilities(sensorId.Value, targetStream, out var capabilities);
        foreach (var pixelSensorCapability in capabilities)
        {
            if (!targetCapabilityTypes.Contains(pixelSensorCapability.CapabilityType))
            {
                continue;
            }

            if (pixelSensorFeature.QueryPixelSensorCapability(sensorId.Value, pixelSensorCapability.CapabilityType, targetStream, out PixelSensorCapabilityRange range) && range.IsValid)
            {
                if (range.CapabilityType == PixelSensorCapabilityType.UpdateRate)
                {
                    var configData      = new PixelSensorConfigData(range.CapabilityType, targetStream);
                    configData.IntValue = DepthRange > 1 ? (uint)LRUpdateRate : (uint)SRUpdateRate;
                    pixelSensorFeature.ApplySensorConfig(sensorId.Value, configData);
                }
                else if (range.CapabilityType == PixelSensorCapabilityType.Format)
                {
                    var configData      = new PixelSensorConfigData(range.CapabilityType, targetStream);
                    configData.IntValue = (uint)range.FrameFormats[UseRawDepth ? 1 : 0];
                    pixelSensorFeature.ApplySensorConfig(sensorId.Value, configData);
                }
                else if (range.CapabilityType == PixelSensorCapabilityType.Resolution)
                {
                    var configData         = new PixelSensorConfigData(range.CapabilityType, targetStream);
                    configData.VectorValue = range.ExtentValues[0];
                    pixelSensorFeature.ApplySensorConfig(sensorId.Value, configData);
                }
                else if (range.CapabilityType == PixelSensorCapabilityType.Depth)
                {
                    var configData = new PixelSensorConfigData(range.CapabilityType, targetStream);
                    configData.FloatValue = DepthRange;
                    pixelSensorFeature.ApplySensorConfig(sensorId.Value, configData);
                }
            }
        }

        StartCoroutine(ConfigureStreamsAndStartSensor());
    }

    private IEnumerator ConfigureStreamsAndStartSensor()
    {

        var configureOperation = pixelSensorFeature.ConfigureSensor(sensorId.Value, configuredStreams.ToArray());
        yield return configureOperation;

        if (configureOperation.DidOperationSucceed)
        {
            Debug.Log("Sensor configured with defaults successfully.");
        }
        else
        {
            Debug.LogError("Failed to configure sensor.");
            yield break;
        }

        Dictionary<uint, PixelSensorMetaDataType[]> supportedMetadataTypes =
        new Dictionary<uint, PixelSensorMetaDataType[]>();

        foreach (uint stream in configuredStreams)
        {
            if (pixelSensorFeature.EnumeratePixelSensorMetaDataTypes(sensorId.Value, stream, out var metaDataTypes))
            {
                supportedMetadataTypes[stream] = metaDataTypes;
            }
        }

        PixelSensorAsyncOperationResult startOperation = pixelSensorFeature.StartSensor(sensorId.Value, configuredStreams, supportedMetadataTypes);
        yield return startOperation;

        if (startOperation.DidOperationSucceed)
        {
            Debug.Log("Sensor started successfully. Monitoring data...");
            StartCoroutine(MonitorSensorData());
        }
        else
        {
            Debug.LogError("Failed to start sensor.");
        }
    }

    private IEnumerator MonitorSensorData()
    {
        Quaternion frameRotation = pixelSensorFeature.GetSensorFrameRotation(sensorId.Value);

        while (pixelSensorFeature.GetSensorStatus(sensorId.Value) == PixelSensorStatus.Started) {
            foreach (uint stream in configuredStreams)
            {
                if (pixelSensorFeature.GetSensorData(sensorId.Value, stream, out var frame, out var metaData,
                        Allocator.Temp, shouldFlipTexture: true))
                {

                    var confidenceMetadata = metaData.OfType<PixelSensorDepthConfidenceBuffer>().FirstOrDefault();
                    if (confidenceMetadata != null)
                    {
                        var flagMetadata = metaData.OfType<PixelSensorDepthFlagBuffer>().FirstOrDefault();
                    }
                }

                float centerDepth = GetDepthAtPixel(frame,  (int)frame.Planes[0].Width / 2, (int)frame.Planes[0].Height / 2);
                if (centerDepth > 0)
                {
                    Camera mainCamera = FindObjectOfType<Camera>();
                    if (mainCamera != null)
                    {
                        Vector3 forwardPosition = mainCamera.transform.position + mainCamera.transform.forward * centerDepth;
                        testObject.transform.position = forwardPosition;                        
                        Debug.Log($"Test object positioned at depth: {centerDepth:F2}m");
                    }
                }

                yield return null;
            }
        }
    }

    public void OnDisable()
    {
        MonoBehaviour camMono = Camera.main.GetComponent<MonoBehaviour>();
        camMono.StartCoroutine(StopSensorCoroutine());
    }

    private IEnumerator StopSensorCoroutine()
    {
        if (sensorId.HasValue)
        {
            PixelSensorAsyncOperationResult stopSensorAsyncResult =
                pixelSensorFeature.StopSensor(sensorId.Value, configuredStreams);

            yield return stopSensorAsyncResult;

            if (stopSensorAsyncResult.DidOperationSucceed)
            {
                Debug.Log("Sensor stopped successfully.");
                pixelSensorFeature.ClearAllAppliedConfigs(sensorId.Value);
                pixelSensorFeature.DestroyPixelSensor(sensorId.Value);
            }
            else
            {
                Debug.LogError("Failed to stop the sensor.");
            }
        }
    }


    // =============
    public static float GetDepthAtPixel(in PixelSensorFrame frame, int x, int y)
    {
        // Validate frame
        if (!frame.IsValid || frame.Planes.Length == 0)
        {
            Debug.LogError("Invalid frame or no planes available");
            return -1f;
        }

        // Get the first plane containing depth data
        ref readonly var plane = ref frame.Planes[0];
        int width  = (int)plane.Width;
        int height = (int)plane.Height;

        // Validate coordinates
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            Debug.LogError($"Pixel coordinates ({x}, {y}) out of bounds. Frame size: {width}x{height}");
            return -1f;
        }

        // Convert ByteData to float array with zero allocations
        ReadOnlySpan<byte> byteSpan = plane.ByteData.AsSpan();
        ReadOnlySpan<float> floatSpan = MemoryMarshal.Cast<byte, float>(byteSpan);

        // Calculate pixel index (row-major order)
        int pixelIndex = y * width + x;

        // Return depth value in meters
        return floatSpan[pixelIndex];
    }
}