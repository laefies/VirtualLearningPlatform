using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] private float dockTolMultiplier = 1.5f;
    protected Spawnable spawnable;

    void Awake() {
        spawnable = GetComponentInParent<Spawnable>();
        PrepareComponents();
    }

    void Update() {
        UpdateComponents();
    }

    public void CheckAutoUndock() {
        MarkerInfo markInfo = spawnable.GetMarkerInfo();
        float distance      = Vector3.Distance(transform.position, markInfo.Pose.position);

        if (distance > markInfo.Size * dockTolMultiplier)
            spawnable.ChangeDockStatus(false);
    }

    public abstract void PrepareComponents();
    public abstract void UpdateComponents();
}
