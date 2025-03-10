using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkManagerHandler : MonoBehaviour
{

    void Start()
    {
        if (Application.isEditor && Application.isPlaying) {
            NetworkManager.Singleton.StartHost();
        } else {

            NetworkManager.Singleton.StartClient();
        }
    }
}