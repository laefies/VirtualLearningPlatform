using System;
using UnityEngine;

/// <summary> Dataclass struct to save information about a marker in the 3D space. </summary>
[Serializable]
public struct MarkerInfo
{
    /// <summary> Unique identifier for the marker. </summary>
    public string Id;

    /// <summary> Position and rotation of the marker in the world space. </summary>
    public Pose Pose;

    /// <summary> Size of the marker in world units. </summary>
    public float Size;
}