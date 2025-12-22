using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Identifies a unique shared object type
/// </summary>
[System.Serializable]
public struct ObjectTypeId : INetworkSerializable, System.IEquatable<ObjectTypeId>
{
    public string value;

    public ObjectTypeId(string id) => value = id;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref value);
    }

    public bool Equals(ObjectTypeId other) => value == other.value;
    public override bool Equals(object obj) => obj is ObjectTypeId other && Equals(other);
    public override int GetHashCode() => value?.GetHashCode() ?? 0;
    public static bool operator ==(ObjectTypeId a, ObjectTypeId b) => a.Equals(b);
    public static bool operator !=(ObjectTypeId a, ObjectTypeId b) => !a.Equals(b);
    public override string ToString() => value;
}

/// <summary>
/// Network-serializable pose data
/// </summary>
[System.Serializable]
public struct NetworkPose : INetworkSerializable
{
    public Vector3 position;
    public Quaternion rotation;

    public NetworkPose(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
    }

    public NetworkPose(Pose pose)
    {
        position = pose.position;
        rotation = pose.rotation;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
    }

    public Pose ToPose() => new Pose(position, rotation);
}

/// <summary>
/// Defines how an object should be positioned relative to its anchor
/// </summary>
[System.Serializable]
public struct AnchorRelativeTransform : INetworkSerializable
{
    public Vector3 positionOffset;
    public Quaternion rotationOffset;
    public float scale;

    public AnchorRelativeTransform(Vector3 pos, Quaternion rot, float scale)
    {
        positionOffset = pos;
        rotationOffset = rot;
        this.scale = scale;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref positionOffset);
        serializer.SerializeValue(ref rotationOffset);
        serializer.SerializeValue(ref scale);
    }

    public static AnchorRelativeTransform Identity => new AnchorRelativeTransform(Vector3.zero, Quaternion.identity, 1f);
}

/// <summary>
/// Information regarding a to-be-placed object whether by 
///          a) AR marker spotting
///          b) VR manual placement
/// </summary>
[System.Serializable]
public struct ObjectPlacementInfo : INetworkSerializable
{
    public ObjectTypeId typeId;
    public NetworkPose localPose;
    public float detectedSize;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref typeId);
        serializer.SerializeValue(ref localPose);
        serializer.SerializeValue(ref detectedSize);
    }
}