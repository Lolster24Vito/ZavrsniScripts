using Assets.Scripts.AI.DOTS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.AI.DOTS
{
    public partial class PathfindingBridgeSystem : SystemBase
    {
        private Dictionary<Entity, Task<List<Vector3>>> _activePathTasks;
        private int MAX_CONCURRENT_TASKS = 5; // Only allow 5 background threads at once

        //int calculatedThisFrame = 0;
        //int MAX_PATHS_PER_FRAME = 3; // Keep this low!

        protected override void OnCreate()
        {
            _activePathTasks = new Dictionary<Entity, Task<List<Vector3>>>();
        }

        protected override void OnUpdate()
        {
            // 1. Handle Completed Tasks
            CompleteFinishedTasks();

            // 2. Start New Tasks (Throttled)
            ScheduleNewTasks();
        }

        private void CompleteFinishedTasks()
        {
            // Create a list to remove keys safely while iterating
            List<Entity> entitiesToRemove = new List<Entity>();

            foreach (var kvp in _activePathTasks)
            {
                Entity entity = kvp.Key;
                Task<List<Vector3>> task = kvp.Value;

                if (task.IsCompleted)
                {
                    entitiesToRemove.Add(entity);

                    if (task.Status == TaskStatus.RanToCompletion && EntityManager.Exists(entity))
                    {
                        List<Vector3> pathPoints = task.Result;

                        // Apply Path to Buffer
                        if (pathPoints != null && pathPoints.Count > 0)
                        {
                            var buffer = EntityManager.GetBuffer<PathElement>(entity);
                            buffer.Clear(); // Ensure it's clean

                            foreach (var p in pathPoints)
                            {
                                buffer.Add(new PathElement { Position = p });
                            }

                            // Reset Path Index
                            var data = EntityManager.GetComponentData<PedestrianData>(entity);
                            data.PathIndex = 0;
                            EntityManager.SetComponentData(entity, data);
                        }
                        EntityManager.RemoveComponent<AStarPathRequestingTag>(entity);
                    }
                    else if (task.IsFaulted)
                    {
                        Debug.LogError($"Pathfinding failed for Entity {entity.Index}: {task.Exception}");
                    }
                }
            }

            // Cleanup dictionary
            foreach (var entity in entitiesToRemove)
            {
                _activePathTasks.Remove(entity);
                // Remove the request tag so we don't request again immediately
                // (Or logic to re-request later if failed)
            }
        }

        private void ScheduleNewTasks()
        {
            // If we are already calculating max paths, wait.
            if (_activePathTasks.Count >= MAX_CONCURRENT_TASKS) return;
            // 1. USE SYSTEMAPI.Query() to gather all relevant entities first
            // This query is safe to use in a SystemBase OnUpdate method.
            var query = SystemAPI.QueryBuilder()
                .WithAll<PedestrianTag>()
                .WithNone<PathRequest>() // Assumed tag to prevent re-scheduling
                .WithNone<PathElement>() // Check if path buffer is empty
                .WithNone<AStarPathRequestingTag>() // OPTIONAL: Tag for entities currently being processed in this system, for better control
                .Build();

            // We get all components and entities that satisfy the query
            var entitiesToProcess = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            // 2. Iterate through the entity array and execute the Mono logic
            foreach (var entity in entitiesToProcess)
            {
                // Double check throttling
                if (_activePathTasks.Count >= MAX_CONCURRENT_TASKS) break;

                // Ensure entity isn't already in the task dictionary (safer check)
                if (_activePathTasks.ContainsKey(entity)) continue;

                // 3. Get all the Mono-World dependent data on the Main Thread (outside any Job or ForEach)
                // Accessing the EntityManager and GetComponentData is safe here.
                var transform = EntityManager.GetComponentData<LocalTransform>(entity);
                var data = EntityManager.GetComponentData<PedestrianData>(entity);

                // Gather Data needed for the thread (Must be thread-safe!)
                Vector3 startPos = transform.Position;
                EntityType type = data.NpcType;

                // Get the start/end nodes on Main Thread to be safe (accessing Mono Singletons)
                // NOTE: You must ensure PedestrianDestinations.Instance.GetRandomNodePoint is thread-safe 
                // OR simply call it here on the main thread, which is safer.
                var startNode = PedestrianDestinations.Instance.GetPoint(startPos);
                var endNode = PedestrianDestinations.Instance.GetRandomNodePoint(type);

                if (startNode != null && endNode != null)
                {
                    // 4. Start Background Task (This is now safely outside the Entities.ForEach context)
                    Task<List<Vector3>> task = Task.Run(() =>
                    {
                        // This runs on a background thread!
                        return PedestrianDestinations.Instance.FindPathSync(startNode, endNode, type);
                    });

                    _activePathTasks.Add(entity, task);

                    // Optional: Add a tag to the entity to mark it as 'requesting' so it gets skipped 
                    // by subsequent queries until the path is complete.
                     EntityManager.AddComponent<AStarPathRequestingTag>(entity); // Use PathRequest if that's your tag
                }
            }

            entitiesToProcess.Dispose(); // Remember to dispose the temporary array
        }
    }
}


















//bakup if i mess up.
/*
protected override void OnUpdate()
{
    // Query entities that need a path (PathIndex == 0 and Buffer is Empty)
    // or use a specific "RequestPathTag"
    int calculatedThisFrame = 0;
    Entities.WithAll<PedestrianTag>().ForEach((Entity entity, ref PedestrianData data, ref DynamicBuffer<PathElement> buffer, in LocalTransform transform) =>
    {
        // 1. Throttle check: If we reached the budget, stop giving new paths this frame.
        if (calculatedThisFrame >= MAX_PATHS_PER_FRAME) return;

        // Logic: If we have no path, ask the Mono world
        if (buffer.IsEmpty)
        {
            // 1. Ask your existing Mono Singleton
            var start = PedestrianDestinations.Instance.GetPoint(transform.Position);
            var end = PedestrianDestinations.Instance.GetRandomNodePoint(EntityType.Pedestrian);

            // This call needs to be synchronous or handle async results
            // For max performance, make a Sync method in your A* that runs fast
            List<Vector3> pathPoints = PedestrianDestinations.Instance.FindPathSync(start, end, data.NpcType);
            // 2. Fill the Buffer
            if (pathPoints != null && pathPoints.Count > 0)
            {
                foreach (var p in pathPoints)
                {
                    buffer.Add(new PathElement { Position = p });
                }
            }
            else
            {

            }
            calculatedThisFrame++;
            //data.PathIndex = 0; //old todo VITO maybe ovo zezne sve
        }
    }).WithoutBurst().Run(); // Main thread only
}
}
}
*/