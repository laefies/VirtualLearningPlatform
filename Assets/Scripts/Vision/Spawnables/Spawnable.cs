using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Spawnable : NetworkBehaviour
{
    private NetworkVariable<bool> _isDocked = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<MarkerInfo> _marker = new NetworkVariable<MarkerInfo>(
        new MarkerInfo() {
            Id = "Default",
            Pose = new Pose(Vector3.zero, Quaternion.identity),
            Size = 1.0f },
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Toggle dockToggle;
    public Transform dockableTransforms;
    public GameObject vrProxy;

    void Start()
    {
        _isDocked.OnValueChanged += (oldValue, newValue) => { dockToggle.isOn = newValue; };

        vrProxy.SetActive(!DeviceManager.Instance.IsAR());

        if (!DeviceManager.Instance.IsAR())
        {
            // In the case of VR users, the object is simulated in an optimal position 
            // for the user - in front of them.
            Transform camera = FindObjectOfType<Camera>().transform;
            MoveSpawnable(new Pose(camera.position + camera.forward * 0.4f, Quaternion.identity), 0.035f);
        }
    }

    public void ChangeDockStatus(bool newDockState)
    {
        if (_isDocked.Value == newDockState) return;

        ChangeDockStatusServerRpc(newDockState);
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