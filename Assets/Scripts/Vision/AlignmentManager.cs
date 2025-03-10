using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// TODO Periodically check for objects that havent been updated in a while.

/// <summary> Handles spawning of objects based on the detected markers. </summary>
public class AlignmentManager : MonoBehaviour
{
    /// <summary> Configuration of prefab mappings for different marker IDs. </summary>
    public DetectionConfiguration config;

    /// <summary> Dictionary of all currently tracked objects, mapped by their IDs. </summary>
    private Dictionary<string, Spawnable> _tracked = new Dictionary<string, Spawnable>();

    /// <summary> Reference to the marker detection manager of the device in-use.</summary>
    private ML2DetectionManager detectionManager;

    /// <summary> Initializes the configuration and sets up marker detection event handling. </summary>
    private void Awake()
    {
        config.Initialize();
        detectionManager = GetComponent<ML2DetectionManager>();

        if (detectionManager != null)
            detectionManager.OnMarkerDetected += ProcessMarker;
    }

    /// <summary> Processes a detected marker, by either spawning a new object or updating an existing one. </summary>
    /// <param name="markerInfo">Information about the detected marker.</param>
    public void ProcessMarker(MarkerInfo markerInfo)
    {
        // Checks if the ID spawns an object
        if (config.GetPrefab(markerInfo.Id) == null)
            return;

        // Either adds a new spawnable or updates an existing one
        if (_tracked.ContainsKey(markerInfo.Id))
            UpdateObject(markerInfo);
        else
            AddObject(markerInfo);
    }

    /// <summary> Spawns and initializes a new object for a detected marker. </summary>
    /// <param name="markerInfo">Information about the marker.</param>
    private void AddObject(MarkerInfo markerInfo)
    {
        Spawnable spawnable = Instantiate(config.GetPrefab(markerInfo.Id)).GetComponent<Spawnable>();
        spawnable.UpdateTransform(markerInfo);
        _tracked[markerInfo.Id] = spawnable;
    }

    /// <summary> Updates the transform of an existing tracked object. </summary>
    /// <param name="markerInfo">Updated marker information.</param>
    private void UpdateObject(MarkerInfo markerInfo)
    {
        _tracked[markerInfo.Id].UpdateTransform(markerInfo);
    }

    /// <summary> Cleans up. Makes sure to unsubscribe from the marker detection event. </summary>
    private void OnDestroy()
    {
        detectionManager.OnMarkerDetected -= ProcessMarker;
    }

}
