using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkManagerHandler : MonoBehaviour
{

    void Start()
    {
        // TODO Designated server
        if (Application.isEditor && Application.isPlaying) {
            NetworkManager.Singleton.StartHost();
        } else {
            Debug.Log("Debug: Trying client");

            NetworkManager.Singleton.StartClient();

            Debug.Log("Debug: After client");
        }
    }
}