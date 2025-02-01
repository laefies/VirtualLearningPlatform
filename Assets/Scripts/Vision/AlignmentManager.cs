using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AlignmentManager : MonoBehaviour
{
    public DetectionConfiguration config;
    private Dictionary<string, Spawnable> _tracked = new Dictionary<string, Spawnable>();

    private void Awake()
    {
        config.Initialize();
    }

    public void ProcessMarker(MarkerInfo markerInfo)
    {
        if (config.GetPrefab(markerInfo.Id) == null)
            return;

        if (_tracked.ContainsKey(markerInfo.Id))
            UpdateObject(markerInfo);
        else
            AddObject(markerInfo);
    }

    private void AddObject(MarkerInfo markerInfo)
    {
        Spawnable spawnable = Instantiate(config.GetPrefab(markerInfo.Id)).GetComponent<Spawnable>();
        spawnable.UpdateTransform(markerInfo);
        _tracked[markerInfo.Id] = spawnable;
    }

    private void UpdateObject(MarkerInfo markerInfo)
    {
        _tracked[markerInfo.Id].UpdateTransform(markerInfo);
    }
}
