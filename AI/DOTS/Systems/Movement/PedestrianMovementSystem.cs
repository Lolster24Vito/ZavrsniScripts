using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Assets.Scripts.AI.DOTS.Components;
using UnityEngine;

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
            state.RequireForUpdate<TileOffsetData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // REMOVED: Automatic path refilling logic.
            // The system now acts purely as a motor. 
            // If the path index reaches the end, it stops and waits for the MonoBehaviour to push new data.
            TileOffsetData tileOffsetData = SystemAPI.GetSingleton<TileOffsetData>();

            var moveJob = new PedestrianMoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                TileOffsets = tileOffsetData.Offsets,
            };

            state.Dependency = moveJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct PedestrianMoveJob : IJobEntity
    {
        public float DeltaTime;

        [ReadOnly] public NativeHashMap<Vector2Int, Vector3> TileOffsets; // Requires using Unity.Collections;

        public void Execute(ref LocalTransform transform, ref PedestrianData pedestrianData,
                            in DynamicBuffer<PathElement> pathBuffer, in TileCoordinate tileCoord)
        {
            // If we have processed all points, stop.
            if (pedestrianData.PathIndex >= pathBuffer.Length)
            {
                return;
            }

            // Get tile offset
            Vector3 tileOffset = Vector3.zero;
            if (TileOffsets.TryGetValue(tileCoord.Value, out tileOffset))
            {
                // Apply tile offset to current position
                float3 currentPosition = transform.Position + (float3)tileOffset;

                // Get target position with offset
                float3 targetPosition = pathBuffer[pedestrianData.PathIndex].Position;

                // Calculate movement
                float3 directionToTarget = targetPosition - currentPosition;
                directionToTarget.y = 0; // Keep on ground plane

                float distanceSq = math.lengthsq(directionToTarget);
                float minDistanceSq = pedestrianData.MinDistanceForCompletion * pedestrianData.MinDistanceForCompletion;

                if (distanceSq <= minDistanceSq)
                {
                    // Reached the point, increment index
                    pedestrianData.PathIndex++;
                    return;
                }

                directionToTarget = math.normalize(directionToTarget);

                // Rotation
                if (math.lengthsq(directionToTarget) > 0.001f)
                {
                    quaternion targetRotation = quaternion.LookRotationSafe(directionToTarget, math.up());
                    transform.Rotation = math.slerp(transform.Rotation, targetRotation, pedestrianData.RotationSpeed * DeltaTime);
                }

                // Translation (without offset)
                transform.Position += directionToTarget * pedestrianData.MoveSpeed * DeltaTime;
            }
        }
    }
}