using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkObjectManager : NetworkBehaviour
{
    public DetectionConfiguration config;

    private Dictionary<string, Spawnable> _tracked = new Dictionary<string, Spawnable>();

    void Start()
    {
        config.Initialize();
    }

    public void ProcessMarker(MarkerInfo markerInfo)
    {
        if (IsClient) ProcessMarkerServerRpc(markerInfo);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ProcessMarkerServerRpc(MarkerInfo markerInfo)
    {
        if (!IsServer) return;

        if (config.GetPrefab(markerInfo.Id) == null)
            return;

        if (_tracked.ContainsKey(markerInfo.Id))
            UpdateObject(markerInfo);
        else
            SpawnObject(markerInfo);
    }

    private void SpawnObject(MarkerInfo markerInfo)
    {
        GameObject spawnedObject = Instantiate(config.GetPrefab(markerInfo.Id));
        spawnedObject.GetComponent<NetworkObject>().Spawn(true);

        Spawnable spawnable = spawnedObject.GetComponent<Spawnable>();
        spawnable.UpdateTransform(markerInfo);
        _tracked[markerInfo.Id] = spawnable;
    }

    private void UpdateObject(MarkerInfo markerInfo)
    {        
        _tracked[markerInfo.Id].UpdateTransform(markerInfo);
    }
}