using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class AlignmentManager : MonoBehaviour
{
    public GameObject MarkerPrefab;
    private Dictionary<string, Spawnable> _tracked = new Dictionary<string, Spawnable>();

    public void ProcessMarker(MarkerInfo markerInfo)
    {
        if (_tracked.ContainsKey(markerInfo.Id)) 
            UpdateObject(markerInfo);
        else 
            AddObject(markerInfo);
    }

    private void AddObject(MarkerInfo markerInfo)
    {
        Spawnable spawnable = Instantiate(MarkerPrefab).GetComponent<Spawnable>();
        spawnable.UpdateTransform(markerInfo);
        _tracked[markerInfo.Id] = spawnable;
    }

    private void UpdateObject(MarkerInfo markerInfo)
    {
        _tracked[markerInfo.Id].UpdateTransform(markerInfo);
    }
}