using Unity.Services.Authentication;
using Unity.Services.Relay.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Core;
using System.Collections;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance{ get; private set;}

    private void Awake() {
        Instance = this;
    }

    public bool IsInRelay()
    {
        var nm = NetworkManager.Singleton;

        if (nm == null || !nm.IsListening)
            return false;

        var transport = nm.GetComponent<UnityTransport>();
        if (transport == null)
            return false;

        return nm.IsHost || nm.IsClient;
    }

    public async Task<string> CreateRelay(int nPlayers = 3)
    {
        try {
            // Create allocation and get the joining code
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(nPlayers);
            string joinCode       = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Relay Created with Join Code => " + joinCode);

            // Handle server data & transportation
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // Start host
            NetworkManager.Singleton.StartHost();

            // Return join code
            return joinCode;
        } catch (RelayServiceException e) {
            Debug.Log("[Relay Error] " + e);
            return null;
        }
    }

    public async Task JoinRelay(string joinCode) {
        try {
            // Attempt to join the relay using join code
            Debug.Log("Joining Relay with Code =>" + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // Handle server data & transportation
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // Start client
            NetworkManager.Singleton.StartClient();
        } catch (RelayServiceException e) {
            Debug.Log("[Relay Error] " + e);
        }
    }

}