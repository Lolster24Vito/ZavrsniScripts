using Assets.Scripts.AI.DOTS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// This system is now completely self-contained and does not rely on TileManager.
public partial class PedestrianTilePositionSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<TileOffsetSingletonTag>();
        RequireForUpdate<TileOffsetData>();
    }
    protected override void OnUpdate()
    {
        // Get the query for our singleton entity and its data ONCE per frame.
        // This is much more efficient than creating it in the job.
        var singletonQuery = GetEntityQuery(typeof(TileOffsetSingletonTag));
        if (singletonQuery.IsEmpty)
        {
            Debug.LogError("TileOffsetSingletonTag not found. The PedestrianTilePositionSystem will not work.");
            return;
        }

        var singletonEntity = singletonQuery.GetSingletonEntity();
        var offsetData = GetComponent<TileOffsetData>(singletonEntity);

        // Now, the job can access offsetData directly. This is Burst-safe.
        Entities.WithAll<PedestrianTag>().ForEach((ref LocalTransform transform, in TileCoordinate tileCoord) =>
        {
            Vector3 tileOffset = Vector3.zero;
            // This is now a Burst-safe call to a NativeHashMap.
            if (offsetData.Offsets.TryGetValue(tileCoord.Value, out tileOffset))
            {
                transform.Position += (float3)tileOffset;
            }
        }).ScheduleParallel();
    }
}