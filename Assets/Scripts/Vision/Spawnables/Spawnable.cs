using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Spawnable : NetworkBehaviour
{
    //private MarkerInfo _marker;
    private NetworkVariable<MarkerInfo> _marker = new NetworkVariable<MarkerInfo>(
        new MarkerInfo()
        {
            Id = "Default", Pose = new Pose(Vector3.zero, Quaternion.identity), Size = 1.0f
        },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public Toggle dockToggle;
    public Transform dockableTransforms;

    public MarkerInfo GetMarkerInfo()
    {
        return _marker.Value;
    }

    public void UpdateTransform(MarkerInfo markerInfo)
    {
        if (IsServer)
            _marker.Value = markerInfo;
    }

    void Update() {
        if (!IsOwner) return;

        transform.SetPositionAndRotation(_marker.Value.Pose.position, _marker.Value.Pose.rotation);
        transform.localScale = Vector3.one * _marker.Value.Size;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeDockStatusServerRpc(bool isDocked)
    {
        Debug.Log("Server dock: " + isDocked);
        dockableTransforms.SetParent(isDocked ? transform : null);
        dockToggle.isOn = isDocked;
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