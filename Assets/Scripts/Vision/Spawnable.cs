using UnityEngine;

/// <summary> Component that handles objects that can be spawned over detected marks. </summary>
public class Spawnable : MonoBehaviour
{
    [SerializeField] private float positionLerpSpeed = 10000f;
    [SerializeField] private float rotationLerpSpeed = 10000f;

    /// <summary> Updates the GameObject's transform using the provided marker information smoothly. </summary>
    /// <param name="markerInfo"> Information regarding the marker that carries this Spawnable. </param>
    public void UpdateTransform(MarkerInfo markerInfo)
    {
        transform.position = Vector3.Lerp(transform.position, markerInfo.Pose.position, Time.deltaTime * positionLerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, markerInfo.Pose.rotation, Time.deltaTime * rotationLerpSpeed);
        transform.localScale = Vector3.one * markerInfo.Size;
    }
}