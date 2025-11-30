using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using Assets.Scripts.AI.DOTS.Components;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PedestrianSpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Get the singleton config
        if (!SystemAPI.TryGetSingleton<PedestrianConfig>(out var config)) return;

        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (request, entity) in SystemAPI.Query<RefRO<SpawnPedestrianRequest>>().WithEntityAccess())
        {
            Entity prefabToUse = request.ValueRO.NpcType == EntityType.Pedestrian
                             ? config.PedestrianPrefab
                             : config.CarPrefab;

            var newEntity = ecb.Instantiate(prefabToUse);
            Vector3 finalPosition = request.ValueRO.Position;
            if (WorldRecenterManager.Instance != null)
            {
                finalPosition -= WorldRecenterManager.Instance.GetRecenterOffset();
            }


            // 2. Set Transform
            ecb.SetComponent(newEntity, LocalTransform.FromPositionRotation(
                finalPosition,
                request.ValueRO.Rotation
            ));
            // Add required components
            ecb.AddComponent(newEntity, new TileCoordinate { Value = request.ValueRO.TileIndex });
            ecb.AddComponent(newEntity, new PedestrianData
            {
                MoveSpeed = 7f,
                RotationSpeed = 20f,
                MinDistanceForCompletion = 1.5f,
                PathIndex = 0,
                NpcType = request.ValueRO.NpcType
            });
            ecb.AddBuffer<PathElement>(newEntity);

            Debug.Log($"[DOTS SPAWN] Spawned {request.ValueRO.NpcType} at {finalPosition} on tile {request.ValueRO.TileIndex}. Entity: {newEntity.Index}");

            ecb.DestroyEntity(entity);
        }
    }
}