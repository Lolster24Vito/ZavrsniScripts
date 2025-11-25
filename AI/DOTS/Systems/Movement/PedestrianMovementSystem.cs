using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Assets.Scripts.AI.DOTS.Components;

namespace AI.DOTS.Systems.Movement
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct PedestrianMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PedestrianTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // REMOVED: Automatic path refilling logic.
            // The system now acts purely as a motor. 
            // If the path index reaches the end, it stops and waits for the MonoBehaviour to push new data.

            var moveJob = new PedestrianMoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            state.Dependency = moveJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct PedestrianMoveJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref LocalTransform transform, ref PedestrianData pedestrianData, in DynamicBuffer<PathElement> pathBuffer)
        {
            // If we have processed all points, stop.
            if (pedestrianData.PathIndex >= pathBuffer.Length)
            {
                return;
            }

            // --- Movement Logic ---
            float3 targetPosition = pathBuffer[pedestrianData.PathIndex].Position;
            float3 currentPosition = transform.Position;
            float3 directionToTarget = targetPosition - currentPosition;

            // Flatten Y for distance check to avoid floating point issues with ground alignment
            float3 directionFlat = directionToTarget;
            directionFlat.y = 0;

            float distanceSq = math.lengthsq(directionFlat);
            float minDistanceSq = pedestrianData.MinDistanceForCompletion * pedestrianData.MinDistanceForCompletion;

            if (distanceSq <= minDistanceSq)
            {
                // Reached the point, increment index to move to the next one
                pedestrianData.PathIndex++;
                return;
            }

            directionToTarget = math.normalize(directionToTarget);

            // --- Rotation ---
            // Only rotate if we are actually moving
            if (math.lengthsq(directionToTarget) > 0.001f)
            {
                quaternion targetRotation = quaternion.LookRotationSafe(directionToTarget, math.up());
                transform.Rotation = math.slerp(transform.Rotation, targetRotation, pedestrianData.RotationSpeed * DeltaTime);
            }

            // --- Translation ---
            transform.Position += directionToTarget * pedestrianData.MoveSpeed * DeltaTime;
        }
    }
}