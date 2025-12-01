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

    private Transform[] allTransforms;
    public GameObject vrProxy;

    void Start()
    {
        // TODO Get dockables, do union when changing visibility
        StoreChildren();
        PrepareSpawnable();
    }

    void StoreChildren()
    {
        if (allTransforms == null)
            allTransforms = GetComponentsInChildren<Transform>(includeInactive: true);
    }


    void PrepareSpawnable()
    {
        // Setup VR proxy visibility
        if (vrProxy != null)
            vrProxy.SetActive(!DeviceManager.Instance.IsAR());

        // Make spawnable immediately visible for VR users
        if (!DeviceManager.Instance.IsAR()) {
            ChangeVisibility(true);
        }        
    }

    void MoveSpawnable(Pose pose, float size)
    {
        transform.SetPositionAndRotation(pose.position, pose.rotation);
        transform.localScale = Vector3.one * size;
    }

    void ChangeVisibility(bool visible)
    {
        if (allTransforms == null) StoreChildren();

        int targetLayer = LayerMask.NameToLayer(visible ? "Default" : "Hidden");
        if (gameObject.layer == targetLayer) return;

        foreach (Transform innerTransform in allTransforms)
            innerTransform.gameObject.layer = targetLayer;
    }

    [ClientRpc]
    public void MoveSpawnableClientRpc(MarkerInfo markerInfo, ClientRpcParams clientRpcParams = default) {
        MoveSpawnable(markerInfo.Pose, markerInfo.Size);
    }

    [ClientRpc]
    public void ChangeVisibilityClientRpc(bool visible, ClientRpcParams clientRpcParams = default) {
        Debug.Log("Made visible by RPC");
        ChangeVisibility(visible);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestOwnershipServerRpc(ServerRpcParams rpcParams = default) {
        NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    public void ReturnOwnershipServerRpc() {
        NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
    }
}