using System;
using UnityEngine;
using Unity.Netcode;

/// <summary> Dataclass struct to save information about a marker in the 3D space. </summary>
[Serializable]
public struct MarkerInfo : INetworkSerializable
{
    /// <summary> Unique identifier for the marker. </summary>
    public string Id;

    /// <summary> Position and rotation of the marker in world space. </summary>
    public Pose Pose;

    /// <summary> Size of the marker in world units. </summary>
    public float Size;

    /// <summary> Implementation of INetworkSerializable to allow Netcode to serialize this struct. </summary>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Id);
        
        Vector3 position = Pose.position;
        Quaternion rotation = Pose.rotation;
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
        
        if (serializer.IsReader)
        {
            Pose = new Pose(position, rotation);
        }
        
        serializer.SerializeValue(ref Size);
    }
}