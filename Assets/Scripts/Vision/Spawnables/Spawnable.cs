using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit; 

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

    void Awake()
    {
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
        vrProxy?.SetActive(!DeviceManager.Instance.IsAR());

        // Spawnable is immediately visible by VR users, while AR users must first spot it
        ChangeVisibility(!DeviceManager.Instance.IsAR(), true);
    }

    void MoveSpawnable(Pose pose, float size)
    {
        transform.SetPositionAndRotation(pose.position, pose.rotation);
        transform.localScale = Vector3.one * size;
    }

    void ChangeVisibility(bool visible) { ChangeVisibility(visible, false); }

    void ChangeVisibility(bool visible, bool force)
    {
        if (allTransforms == null) StoreChildren();

        int targetLayer = LayerMask.NameToLayer(visible ? "Default" : "Hidden");
        if (!force && targetLayer == gameObject.layer) return;

        foreach (Transform t in allTransforms) {
            GameObject go = t.gameObject;

            // Toggle components to block interaction:
            //  - UI (Image, RawImage, TextMeshProUGUI, etc.);
            //  - XR Interactables (grab, poke, ray);
            //  - Colliders;
            foreach (Graphic g in go.GetComponents<Graphic>()) g.enabled = visible;
            foreach (Collider c in go.GetComponents<Collider>()) c.enabled = visible;
            foreach (XRBaseInteractable i in go.GetComponents<XRBaseInteractable>()) i.enabled = visible;
            
            // Update layer
            go.layer = targetLayer;
        }
    }

    [ClientRpc]
    public void UpdateSpawnableClientRpc(MarkerInfo markerInfo, ClientRpcParams clientRpcParams = default) {
        MoveSpawnable(markerInfo.Pose, markerInfo.Size);
        ChangeVisibility(true);
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