using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunSpawnable : MonoBehaviour
{
    [SerializeField] private float positionLerpSpeed  = .5f;
    [SerializeField] private float rotationLerpSpeed  = .5f;
    [SerializeField] private float dockMultiplier     = 1.5f;
    [SerializeField] private float disableTime        = .5f;

    private bool _isGrabbed  = false;
    private bool _withinDock = true;
    protected MarkerInfo _marker;
    private float _lastSeen  = 0f;
    private Rigidbody _rb;

    protected void Awake()
    {
        _rb = GetComponent<Rigidbody>(); 
    }

    public void SetIsGrabbed(bool value)
    {
        _isGrabbed = value;
        if (!_isGrabbed) 
            checkDockStatus();
    }

    private void checkDockStatus() {
        _withinDock = Vector3.Distance(transform.position, _marker.Pose.position) < _marker.Size * dockMultiplier;
    }

    /// <summary> Updates the GameObject's transform using the provided marker information smoothly. </summary>
    /// <param name="markerInfo"> Information regarding the marker that carries this Spawnable. </param>
    public void UpdateTransform(MarkerInfo markerInfo)
    {
        _lastSeen = 0f;
        gameObject.SetActive(true); 

        if (!_isGrabbed) {

            if (_withinDock) {
                _rb.MovePosition(Vector3.Lerp(transform.position, markerInfo.Pose.position, Time.deltaTime * positionLerpSpeed));
                _rb.MoveRotation(Quaternion.Slerp(transform.rotation, markerInfo.Pose.rotation, Time.deltaTime * rotationLerpSpeed));
                transform.localScale = Vector3.one * markerInfo.Size;
            } else {
                checkDockStatus();
            }
        }
        _marker   = markerInfo;
    }

    protected void Update()
    {
        _lastSeen += Time.deltaTime;

        if (!_isGrabbed && _withinDock && _lastSeen >= disableTime)
            gameObject.SetActive(false); 
    }

}