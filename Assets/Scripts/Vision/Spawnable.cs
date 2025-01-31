using UnityEngine;

public class Spawnable : MonoBehaviour
{
    public void UpdateTransform(MarkerInfo markerInfo)
    {
        transform.position = markerInfo.Pose.position;
        transform.rotation = markerInfo.Pose.rotation;
        transform.localScale = Vector3.one * markerInfo.Size;
    }
}