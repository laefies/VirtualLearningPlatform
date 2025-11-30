using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Spawnable : NetworkBehaviour
{
    [Header("Marker Configuration")]
    private NetworkVariable<MarkerInfo> _marker = new NetworkVariable<MarkerInfo>(
        new MarkerInfo() {
            Id = "Default",
            Pose = new Pose(Vector3.zero, Quaternion.identity),
            Size = 1.0f
        },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public GameObject vrProxy;

    void Start() {
        // Setup VR proxy visibility
        if (vrProxy != null)
            vrProxy.SetActive(!DeviceManager.Instance.IsAR());

        // TODO Position spawnable for VR users
        // if (!DeviceManager.Instance.IsAR() && IsServer)
        //     PositionForVRUser();
    }

    [ClientRpc]
    public void UpdateSpawnableClientRpc(MarkerInfo markerInfo, ClientRpcParams clientRpcParams = default)
    {
        MoveSpawnable(markerInfo.Pose, markerInfo.Size);
    }

    void MoveSpawnable(Pose pose, float size)
    {
        transform.SetPositionAndRotation(pose.position, pose.rotation);
        transform.localScale = Vector3.one * size;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestOwnershipServerRpc(ServerRpcParams rpcParams = default)
    {
        NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    public void ReturnOwnershipServerRpc()
    {
        NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
    }
}