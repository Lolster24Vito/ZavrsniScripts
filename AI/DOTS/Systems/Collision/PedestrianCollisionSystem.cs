using Assets.Scripts.AI.DOTS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct PedestrianCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        int playerLayerIndex = 12; 
        int bulletLayerIndex = 13;

        uint targetLayerMask = (1u << playerLayerIndex) | (1u << bulletLayerIndex);
        state.Dependency = new PedestrianTriggerJob
        {
            PedestrianLookup = SystemAPI.GetComponentLookup<PedestrianTag>(true),
            RagdollTriggerLookup = SystemAPI.GetComponentLookup<TriggerRagdollTag>(),
            // We need to look up Colliders to check their Layer
            ColliderLookup = SystemAPI.GetComponentLookup<PhysicsCollider>(true),

            TargetLayerMask = targetLayerMask
        }.Schedule(simulation, state.Dependency);
    }

    [BurstCompile]
    public struct PedestrianTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<PedestrianTag> PedestrianLookup;
        public ComponentLookup<TriggerRagdollTag> RagdollTriggerLookup;
        [ReadOnly] public ComponentLookup<PhysicsCollider> ColliderLookup;

        public uint TargetLayerMask;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            bool isAPed = PedestrianLookup.HasComponent(entityA);
            bool isBPed = PedestrianLookup.HasComponent(entityB);

            // Case 1: A is Pedestrian, B is [Player OR Bullet]
            if (isAPed && !isBPed)
            {
                if (CheckLayer(entityB))
                {
                    EnableRagdoll(entityA);
                }
            }
            // Case 2: B is Pedestrian, A is [Player OR Bullet]
            else if (isBPed && !isAPed)
            {
                if (CheckLayer(entityA))
                {
                    EnableRagdoll(entityB);
                }
            }
        }

        private bool CheckLayer(Entity entity)
        {
            // If the entity has a collider, check its filter
            if (ColliderLookup.HasComponent(entity))
            {
                var collider = ColliderLookup[entity];
                // BelongsTo describes "What Layer am I?"
                // We check if the entity's layer is inside our Target Mask
                return (collider.Value.Value.GetCollisionFilter().BelongsTo & TargetLayerMask) != 0;
            }
            return false;
        }

        private void EnableRagdoll(Entity entity)
        {
            if (RagdollTriggerLookup.HasComponent(entity))
            {
                RagdollTriggerLookup.SetComponentEnabled(entity, true);
            }
        }
    }
}