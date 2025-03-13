using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Netcode;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] private float dockTolMultiplier = 1.5f;
    protected Spawnable spawnable;

    void Awake() {
        spawnable = GetComponentInParent<Spawnable>();
        PrepareComponents();

        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.onSelectEntered.AddListener(OnGrab);
        grabInteractable.onSelectExited.AddListener(OnRelease);
    }

    private void OnGrab(XRBaseInteractor interactor)
    {
        if (!spawnable.GetComponent<NetworkObject>().IsOwner)
        {
            spawnable.RequestOwnershipServerRpc();
        }
    }

    private void OnRelease(XRBaseInteractor interactor)
    {
        spawnable.ReturnOwnershipServerRpc();
        CheckAutoUndock();
    }

    public void CheckAutoUndock() {
        MarkerInfo markInfo = spawnable.GetMarkerInfo();
        float distance      = Vector3.Distance(transform.position, markInfo.Pose.position);

        if (distance > markInfo.Size * dockTolMultiplier)
            spawnable.ChangeDockStatusServerRpc(false);
    }

    void Update() {
        UpdateComponents();
    }

    public abstract void PrepareComponents();
    public abstract void UpdateComponents();
}
