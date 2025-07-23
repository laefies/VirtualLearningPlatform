using System.Collections;
using System.Collections.Generic;
using MagicLeap.Android;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.PixelSensors;
using System;

public class ML2CameraSensorTest : MonoBehaviour
{
    [Header("Stream Configuration")]
    public bool useStream0 = true;
    public bool useStream1 = true;

    [Header("Render Settings")]
    [SerializeField]
    private string pixelSensorName = "Picture Center";

    private string requiredPermission = MLPermission.Camera;
    // Array to hold textures for each stream
    private Texture2D[] streamTextures = new Texture2D[2];
    // Optional sensor ID, used to interact with the specific sensor
    private PixelSensorId? sensorId;
    // List to keep track of which streams have been configured
    private readonly List<uint> configuredStreams = new List<uint>();
    // Reference to the Magic Leap Pixel Sensor Feature
    private MagicLeapPixelSensorFeature pixelSensorFeature;

    private Vector2 principalPoint;
    private Vector2 focalLength;
    private bool setIntrinsics;

    private byte[] _lastFrameData;
    public byte[] GetLastFrame() { return _lastFrameData; }

    private void Start()
    {
        InitializePixelSensorFeature();
    }

    private void InitializePixelSensorFeature()
    {
        // Get the Magic Leap Pixel Sensor Feature from the OpenXR settings
        pixelSensorFeature = OpenXRSettings.Instance.GetFeature<MagicLeapPixelSensorFeature>();
        if (pixelSensorFeature == null || !pixelSensorFeature.enabled)
        {
            Debug.LogError("Pixel Sensor Feature Not Found or Not Enabled!");
            enabled = false;
            return;
        }
        RequestPermission(MLPermission.Camera);
    }

    // Method to request a specific permission
    private void RequestPermission(string permission)
    {
        Permissions.RequestPermission(permission, OnPermissionGranted, OnPermissionDenied);
    }

    // Callback for when permission is granted
    private void OnPermissionGranted(string permission)
    {
        if (Permissions.CheckPermission(requiredPermission))
        {
            FindAndInitializeSensor();
        }
    }

    // Callback for when permission is denied
    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Permission Denied: {permission}");
        enabled = false;
    }

    // Find the sensor by name and try to initialize it
    private void FindAndInitializeSensor()
    {
        List<PixelSensorId> sensors = pixelSensorFeature.GetSupportedSensors();
        int index = sensors.FindIndex(x => x.SensorName.Contains(pixelSensorName));

        if (index <= 0)
        {
            Debug.LogError($"{pixelSensorName} sensor not found.");
            return;
        }

        sensorId = sensors[index];

        // Subscribe to sensor availability changes
        pixelSensorFeature.OnSensorAvailabilityChanged += OnSensorAvailabilityChanged;
        TryInitializeSensor();
    }

    // Handle changes in sensor availability, tries to initialize the sensor if it becomes available 
    private void OnSensorAvailabilityChanged(PixelSensorId id, bool available)
    {
        if (id == sensorId && available)
        {
            Debug.Log($"Sensor became available: {id.SensorName}");
            TryInitializeSensor();
        }
    }

    // Try to create and initialize the sensor
    private void TryInitializeSensor()
    {
        Debug.Log("TryInitialize Sensor");
        if (sensorId.HasValue && pixelSensorFeature.CreatePixelSensor(sensorId.Value))
        {
            Debug.Log("Sensor created successfully.");
            ConfigureSensorStreams();
        }
        else
        {
            Debug.LogError("Failed to create sensor. Will retry when available.");
        }
    }

    // Configure streams based on the sensor capabilities
    private void ConfigureSensorStreams()
    {
        if (!sensorId.HasValue)
        {
            Debug.LogError("Sensor Id was not set.");
            return;
        }

        uint streamCount = pixelSensorFeature.GetStreamCount(sensorId.Value);
        if (useStream1 && streamCount < 2 || useStream0 && streamCount < 1)
        {
            Debug.LogError("target Streams are not available from the sensor.");
            return;
        }

        for (uint i = 0; i < streamCount; i++)
        {
            if ((useStream0 && i == 0) || (useStream1 && i == 1))
            {
                configuredStreams.Add(i);
            }
        }

        StartCoroutine(StartSensorStream());
    }

    // Coroutine to configure stream and start sensor streams
    private IEnumerator StartSensorStream()
    {
        // Configure the sensor with default configuration
        PixelSensorAsyncOperationResult configureOperation =
            pixelSensorFeature.ConfigureSensorWithDefaultCapabilities(sensorId.Value, configuredStreams.ToArray());

        yield return configureOperation;

        if (!configureOperation.DidOperationSucceed)
        {
            Debug.LogError("Failed to configure sensor.");
            yield break;
        }

        Debug.Log("Sensor configured with defaults successfully.");
        

        ////////////////////
        Dictionary<uint, PixelSensorMetaDataType[]> supportedMetadataTypes = new Dictionary<uint, PixelSensorMetaDataType[]>();

        foreach (uint stream in configuredStreams)
        {
            if (pixelSensorFeature.EnumeratePixelSensorMetaDataTypes(sensorId.Value, stream, out var metaDataTypes))
            {
                supportedMetadataTypes[stream] = metaDataTypes;
            }
        }
        /// 

        // Start the sensor with the default configuration and specify that all of the meta data should be requested.
        var sensorStartAsyncResult =
            pixelSensorFeature.StartSensor(sensorId.Value, configuredStreams, supportedMetadataTypes);

        yield return sensorStartAsyncResult;

        if (!sensorStartAsyncResult.DidOperationSucceed)
        {
            Debug.LogError("Stream could not be started.");
            yield break;
        }

        Debug.Log("Stream started successfully.");
        yield return ProcessSensorData();
    }

    private IEnumerator ProcessSensorData()
    {
        while (sensorId.HasValue && pixelSensorFeature.GetSensorStatus(sensorId.Value) == PixelSensorStatus.Started)
        {
            foreach (var stream in configuredStreams)
            {
                // In this example, the meta data is not used.
                if (pixelSensorFeature.GetSensorData(
                        sensorId.Value, stream, out var frame,
                        out PixelSensorMetaData[] currentFrameMetaData,
                        Allocator.Temp, shouldFlipTexture: true))
                {
                    // Pose sensorPose = pixelSensorFeature.GetSensorPose(sensorId.Value);
                    // Debug.Log($"RGB Pixel Sensor Pose: Position {sensorPose.position} Rotation: {sensorPose.rotation}");
                    
                    _lastFrameData = new byte[frame.Planes[0].ByteData.Length];
                    frame.Planes[0].ByteData.CopyTo(_lastFrameData);
                }
            }
            yield return null;
        }
    }

    private void OnDisable()
    {
        var camMono = Camera.main.GetComponent<MonoBehaviour>();
        camMono.StartCoroutine(StopSensor());
    }

    private IEnumerator StopSensor()
    {
        if (sensorId.HasValue)
        {
            var stopSensorAsyncResult = pixelSensorFeature.StopSensor(sensorId.Value, configuredStreams);
            yield return stopSensorAsyncResult;
            if (stopSensorAsyncResult.DidOperationSucceed)
            {
                pixelSensorFeature.DestroyPixelSensor(sensorId.Value);
                Debug.Log("Sensor stopped and destroyed successfully.");
            }
            else
            {
                Debug.LogError("Failed to stop the sensor.");
            }
        }
    }

}