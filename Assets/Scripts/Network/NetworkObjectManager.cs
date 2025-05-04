using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkObjectManager : NetworkBehaviour
{
    public DetectionConfiguration config;
    private Dictionary<string, Spawnable> _tracked = new Dictionary<string, Spawnable>();

    public static NetworkObjectManager Instance{ get; private set;}

    private void Awake() {
        Instance = this;
    }

    void Start()
    {
        config.Initialize();
    }

    public async void ProcessMarker(MarkerInfo markerInfo)
    {
        if (IsClient) ProcessMarkerServerRpc(markerInfo);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ProcessMarkerServerRpc(MarkerInfo markerInfo)
    {
        if (!IsServer) return;

        if (config.GetPrefab(markerInfo.Id) == null)
            return;

        if (!_tracked.ContainsKey(markerInfo.Id))
            SpawnObject(markerInfo);

        UpdateObject(markerInfo);
    }

    private void SpawnObject(MarkerInfo markerInfo)
    {
        GameObject spawnedObject = Instantiate(config.GetPrefab(markerInfo.Id), markerInfo.Pose.position, markerInfo.Pose.rotation);
        spawnedObject.transform.localScale = Vector3.one * markerInfo.Size;

        spawnedObject.GetComponent<NetworkObject>().Spawn(true);
        _tracked[markerInfo.Id] = spawnedObject.GetComponent<Spawnable>();
    }

    private void UpdateObject(MarkerInfo markerInfo)
    {        
        _tracked[markerInfo.Id].UpdateTransform(markerInfo);
    }
}