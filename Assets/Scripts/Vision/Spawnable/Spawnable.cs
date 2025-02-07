using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Spawnable : MonoBehaviour
{
    [SerializeField] private float positionLerpSpeed = .5f;
    [SerializeField] private float rotationLerpSpeed = .5f;

    private bool _isDocked = true;
    private MarkerInfo _marker;

    public MarkerInfo GetMarkerInfo()
    {
        return _marker;
    }

    public void SetIsDocked(bool value)
    {
        _isDocked = value;
    }

    public void UpdateTransform(MarkerInfo markerInfo)
    {
        _marker = markerInfo;
    }

    void Update()
    {
        if (_isDocked) {
            transform.position   = Vector3.Lerp(transform.position, _marker.Pose.position, Time.deltaTime * positionLerpSpeed);
            transform.rotation   = Quaternion.Slerp(transform.rotation, _marker.Pose.rotation, Time.deltaTime * rotationLerpSpeed);
            transform.localScale = Vector3.one * _marker.Size;
        }
    }

}