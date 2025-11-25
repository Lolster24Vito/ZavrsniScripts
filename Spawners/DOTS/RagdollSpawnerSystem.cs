using Assets.Scripts.AI.DOTS.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Must be a class (SystemBase) to use Instantiate(GameObject)
public partial class RagdollSpawnerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Get the command buffer to destroy entities later
        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(World.Unmanaged);

        // Query all entities that have the TriggerRagdollTag enabled
        foreach (var (transform, data, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRO<PedestrianData>>() 
                     .WithAll<TriggerRagdollTag>()
                     .WithEntityAccess())
        {
            if (data.ValueRO.NpcType == EntityType.Pedestrian)
            {
                GameObject ragdollGO = NpcPoolManager.Instance.GetPedestrianRagdoll();

                ragdollGO.transform.position = transform.ValueRO.Position;
                ragdollGO.transform.rotation = transform.ValueRO.Rotation;
                ragdollGO.SetActive(true);

                var ragdollScript = ragdollGO.GetComponent<Ragdoll>();
                if (ragdollScript != null)
                {
                    // Calculate Force:
                    // You can use the entity's rotation (Forward) or a hardcoded vector.
                    // transform.ValueRO.Forward() requires "using Unity.Transforms;"
                    float3 forceDirection = transform.ValueRO.Forward();

                    // Or if you want to simulate an impact from a specific direction (like the player),
                    // you would need to pass that data via the TriggerRagdollTag component.

                    ragdollScript.TriggerRagdoll(50f, transform.ValueRO.Position, forceDirection);
                    ecb.DestroyEntity(entity);
                }
            }
            else if (data.ValueRO.NpcType == EntityType.Car)
            {
                // CAR LOGIC: 
                // For now, just destroy the car entity (poof!).
                // Later you can add NpcPoolManager.Instance.GetCarExplosion() here.
                Debug.Log("Car hit! Destroying entity.");
                //todo car is not destoryed
            }
            // 1. Spawn the GameObject Ragdoll
            // You should use your NpcPoolManager here!
           

            //OLD CODE delete maybe?
            // 2. Apply Force (Optional)
            // You can calculate force based on player position here
            //  var rb = ragdollGO.GetComponentInChildren<Rigidbody>();
            //   if (rb != null) rb.AddForce(Vector3.forward * 50f, ForceMode.Impulse);

            // 2. Trigger the ragdoll logic


        }
    }
}