using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Spawnable : NetworkBehaviour
{
    private NetworkVariable<bool> _isDocked = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<MarkerInfo> _marker = new NetworkVariable<MarkerInfo>(
        new MarkerInfo()
        {
            Id = "Default", Pose = new Pose(Vector3.zero, Quaternion.identity), Size = 1.0f
        },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public Toggle dockToggle;
    public Transform dockableTransforms;

    void Start() {
        _isDocked.OnValueChanged += (oldValue, newValue) => {
            dockToggle.isOn = newValue;
        };
    }

    public void ChangeDockStatus(bool newDockState) {
        if (_isDocked.Value == newDockState) return;

        ChangeDockStatusServerRpc(newDockState);
    }

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
    private void ChangeDockStatusServerRpc(bool newDockState)
    {
        dockableTransforms.SetParent(newDockState ? transform : null);
        _isDocked.Value = newDockState;
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