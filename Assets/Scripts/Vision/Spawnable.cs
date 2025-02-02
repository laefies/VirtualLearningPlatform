using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary> Component that handles objects that can be spawned over detected marks. </summary>
public class Spawnable : MonoBehaviour
{
    [SerializeField] private float positionLerpSpeed = 10000f;
    [SerializeField] private float rotationLerpSpeed = 10000f;

    private bool _isGrabbed  = false;
    private bool _withinDock = true;

    private MarkerInfo _marker;

    public void SetIsGrabbed(bool value)
    {
        _isGrabbed = value;
        if (!_isGrabbed) 
            checkDockStatus();
    }

    private void checkDockStatus() {
        _withinDock = Vector3.Distance(transform.position, _marker.Pose.position) < _marker.Size;
    }

    /// <summary> Updates the GameObject's transform using the provided marker information smoothly. </summary>
    /// <param name="markerInfo"> Information regarding the marker that carries this Spawnable. </param>
    public void UpdateTransform(MarkerInfo markerInfo)
    {
        if (!_isGrabbed) {

            if (_withinDock) {
                transform.position   = Vector3.Lerp(transform.position, markerInfo.Pose.position, Time.deltaTime * positionLerpSpeed);
                transform.rotation   = Quaternion.Slerp(transform.rotation, markerInfo.Pose.rotation, Time.deltaTime * rotationLerpSpeed);
                transform.localScale = Vector3.one * markerInfo.Size;
            } else {
                checkDockStatus();
            }

        }

        this._marker = markerInfo;
    }
}