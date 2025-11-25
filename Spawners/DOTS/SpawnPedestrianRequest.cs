using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SpawnPedestrianRequest : IComponentData
{
    public Entity PrefabToSpawn;
    public float3 Position;
    public quaternion Rotation;
    public Vector2Int TileIndex; // To keep track of your tiles
    public EntityType NpcType;
}