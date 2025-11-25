using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;

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


            // 2. Set Transform
            ecb.SetComponent(newEntity, LocalTransform.FromPositionRotation(
                request.ValueRO.Position,
                request.ValueRO.Rotation
            ));

            // 3. Destroy the request entity so we don't spawn infinite copies
            ecb.DestroyEntity(entity);
        }
    }
}