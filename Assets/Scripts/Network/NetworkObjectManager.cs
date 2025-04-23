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

        if (_tracked.ContainsKey(markerInfo.Id))
            UpdateObject(markerInfo);
        else
            SpawnObject(markerInfo);
    }

    private void SpawnObject(MarkerInfo markerInfo)
    {
        Debug.Log("[NOM] Hiii");
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