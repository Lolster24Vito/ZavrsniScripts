using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// A component to hold our tile offset data in a DOTS-friendly format
public struct TileOffsetData : IComponentData
{
    // A NativeHashMap is the DOTS equivalent of a Dictionary and is Burst-compatible
    public NativeHashMap<Vector2Int, Vector3> Offsets;
}