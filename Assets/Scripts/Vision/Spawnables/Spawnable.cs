using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class Spawnable : MonoBehaviour
{
    [SerializeField] private float positionLerpSpeed = .5f;
    [SerializeField] private float rotationLerpSpeed = .5f;

    private MarkerInfo _marker;
    public Toggle dockToggle;
    public Transform dockableTransforms;

    public MarkerInfo GetMarkerInfo()
    {
        return _marker;
    }

    public void ChangeDockStatus(bool isDocked)
    {
        dockableTransforms.SetParent(isDocked ? transform : null);
        dockToggle.isOn = isDocked;
    }

    public void UpdateTransform(MarkerInfo markerInfo)
    {
        _marker = markerInfo;
    }

    void Update()
    {
        // transform.position = Vector3.Lerp(transform.position, _marker.Pose.position, Time.deltaTime * positionLerpSpeed);
        // transform.rotation = Quaternion.Slerp(transform.rotation, _marker.Pose.rotation, Time.deltaTime * rotationLerpSpeed);
        // transform.localScale = Vector3.one * _marker.Size;
        transform.SetPositionAndRotation(_marker.Pose.position, _marker.Pose.rotation);
        transform.localScale = Vector3.one * _marker.Size;
    }

}