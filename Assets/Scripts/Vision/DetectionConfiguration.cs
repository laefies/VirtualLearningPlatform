using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicLeap.OpenXR.Features.MarkerUnderstanding;

[CreateAssetMenu(fileName = "DetectionConfiguration", menuName = "Configuration/DetectionConfig")]
public class DetectionConfiguration : ScriptableObject
{
    public MarkerType MarkerType = MarkerType.Aruco;
    public ArucoType ArucoDictionary = ArucoType.Dictionary_5x5_50;
    public bool EstimateArucoLength = true;

    [System.Serializable]
    public struct MarkerMapping
    {
        public string MarkerId;
        public GameObject SpawnablePrefab;
    }

    public List<MarkerMapping> MarkerMappings;

    private Dictionary<string, GameObject> _markerLookup;

    public void Initialize()
    {
        _markerLookup = new Dictionary<string, GameObject>();
        foreach (var mapping in MarkerMappings)
        {
            _markerLookup[mapping.MarkerId] = mapping.SpawnablePrefab;
        }
    }

    public GameObject GetPrefab(string markerId)
    {
        _markerLookup.TryGetValue(markerId, out var prefab);
        return prefab;
    }
}
