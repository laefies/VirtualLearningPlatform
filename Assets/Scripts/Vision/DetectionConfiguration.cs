using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicLeap.OpenXR.Features.MarkerUnderstanding;
using System.Linq;

/// <summary>  Scriptable Object that handles configuring logic for detection - the settings and the prefab mappings. </summary>
[CreateAssetMenu(fileName = "DetectionConfiguration", menuName = "Configuration/DetectionConfig")]
public class DetectionConfiguration : ScriptableObject
{
    public MarkerType MarkerType = MarkerType.Aruco;
    public ArucoType ArucoDictionary = ArucoType.Dictionary_5x5_50;
    public bool EstimateArucoLength = true;

    /// <summary>  Represents a mapping between a marker ID and its corresponding spawnable prefab. </summary>
    [System.Serializable]
    public struct MarkerMapping
    {
        /// <summary> The unique identifier of the marker. </summary>
        public string MarkerId;

        /// <summary> The prefab to spawn when said marker is detected. </summary>
        public GameObject SpawnablePrefab;
    }

    /// <summary> List of marker-to-prefab mappings, allowing configuration via Inspector. </summary>
    public List<MarkerMapping> MarkerMappings;

    /// <summary> Auxiliar internal dictionary to optimize lookups. </summary>
    private Dictionary<string, GameObject> _markerLookup;

    /// <summary> Initializes the internal lookup dictionary. </summary>
    public void Initialize()
    {
        _markerLookup = MarkerMappings.ToDictionary(m => m.MarkerId, m => m.SpawnablePrefab);
    }

    /// <summary> Gets the prefab associated with a certain marker ID. </summary>
    /// <param name="markerId">ID of the marker to look up.</param>
    /// <returns>Associated prefab (or null if no mapping exists).</returns>
    public GameObject GetPrefab(string markerId) => _markerLookup.TryGetValue(markerId, out var prefab) ? prefab : null;
}
