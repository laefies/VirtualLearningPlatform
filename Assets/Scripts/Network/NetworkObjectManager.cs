using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkObjectManager : NetworkBehaviour
{
    public DetectionConfiguration config;

    private Dictionary<string, Spawnable> _tracked = new Dictionary<string, Spawnable>();

    private void Awake()
    {
        config.Initialize();
    }

    public void ProcessMarker(MarkerInfo markerInfo)
    {
        if (IsClient) {

            ProcessMarkerServerRpc(markerInfo);

            GameObject[] allObjects = FindObjectsOfType<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj.transform.parent == null) // Only objects without a parent
                {
                    Debug.Log(obj.name + ": " + obj.transform.position);
                }
            }
        }
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
        GameObject obj = Instantiate(config.GetPrefab(markerInfo.Id));
        obj.GetComponent<NetworkObject>().Spawn(true);

        Spawnable spawnable = obj.GetComponent<Spawnable>();
        spawnable.UpdateTransform(markerInfo);
        _tracked[markerInfo.Id] = spawnable;
    }

    private void UpdateObject(MarkerInfo markerInfo)
    {
        _tracked[markerInfo.Id].UpdateTransform(markerInfo);
    }
}