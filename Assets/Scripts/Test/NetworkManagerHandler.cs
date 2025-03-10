using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkManagerHandler : MonoBehaviour
{
    [SerializeField] private bool isHost;

    void Start()
    {
        Debug.Log("Debug: Hii");
        if (isHost) {
            NetworkManager.Singleton.StartHost();
        } else {
            Debug.Log("Debug: Trying client");

            NetworkManager.Singleton.StartClient();

            Debug.Log("Debug: After client");
        }
    }
}