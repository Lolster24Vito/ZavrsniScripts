using Assets.Scripts.AI.DOTS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(PedestrianSpawnSystem))]
public partial struct LinkSpawnedEntitiesSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        int linkedCount = 0;

        foreach (var (transform, entity) in
            SystemAPI.Query<RefRO<LocalTransform>>()
            .WithAll<PedestrianTag>()
            .WithNone<LinkedGameObject>()
            .WithEntityAccess())
        {
            // Get the NPC type from the entity
            EntityType npcType = EntityType.Pedestrian; // Default

            if (SystemAPI.HasComponent<PedestrianData>(entity))
            {
                // We use GetComponentRO (Read Only) for efficiency
                npcType = SystemAPI.GetComponentRO<PedestrianData>(entity).ValueRO.NpcType;
            }

            // Get the appropriate prefab from the pool
            GameObject npcGameObject = npcType == EntityType.Pedestrian ?
                NpcPoolManager.Instance.GetPedestrian() :
                NpcPoolManager.Instance.GetCar();

            if (npcGameObject != null)
            {
                // 1. Set the GameObject's position to match the DOTS entity
                npcGameObject.transform.position = transform.ValueRO.Position;
                npcGameObject.transform.rotation = transform.ValueRO.Rotation;

                // 2. Link the DOTS entity to the GameObject
                var pedestrianScript = npcGameObject.GetComponent<Pedestrian>();

                // Ensure we find the Authoring component to inject the Entity ID
                if (pedestrianScript != null && pedestrianScript.TryGetComponent<PedestrianAuthoring>(out var authoring))
                {
                    // This is the crucial link that allows the MonoBehaviour to find its Entity
                    authoring.BakedEntity = entity;

                    // Add a tag to the DOTS entity so we don't link it again
                    ecb.AddComponent(entity, new LinkedGameObject());
                    linkedCount++;
                }
                else
                {
                    // If something is wrong, return it to the pool immediately
                    Debug.LogError($"[ENTITY LINK] Spawned GameObject is missing PedestrianAuthoring or Pedestrian script!");

                    // FIXED: Used 'npcType' here instead of the undefined 'pedData'
                    if (npcType == EntityType.Pedestrian)
                    {
                        NpcPoolManager.Instance.ReleasePedestrian(npcGameObject);
                    }
                    else
                    {
                        NpcPoolManager.Instance.ReleaseCar(npcGameObject);
                    }
                }
            }
        }

        if (linkedCount > 0)
        {
            Debug.Log($"[ENTITY LINK] Linked {linkedCount} entities to GameObjects");
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

// ---------------------------------------------------------
// Component Definition
// ---------------------------------------------------------

// This is a "Tag Component". It contains no data but is used 
// to mark Entities that have successfully been linked to a GameObject.
public struct LinkedGameObject : IComponentData
{
}