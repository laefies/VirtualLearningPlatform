using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeviceManager : MonoBehaviour
{
    protected async void HandlePlayerLeaving()
    {
        LobbyManager.Instance.LeaveLobby();
    }
}