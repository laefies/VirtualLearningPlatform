using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkManagerHandler : MonoBehaviour
{
    public GameObject objectManager;

    void Start()
    {
        if (Application.isEditor && Application.isPlaying) {
            NetworkManager.Singleton.StartHost();
            GameObject obj = Instantiate(objectManager);
            obj.GetComponent<NetworkObject>().Spawn();
        } else {
            NetworkManager.Singleton.StartClient();
        }

    }
}