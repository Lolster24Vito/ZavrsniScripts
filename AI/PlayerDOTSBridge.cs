// In PlayerDOTSBridge.cs

using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

public class PlayerDOTSBridge : MonoBehaviour
{
    private Entity ghostEntity = Entity.Null;
    private EntityManager entityManager;

    private void Start()
    {
        // We will spawn the ghost in a coroutine to allow the DOTS world to initialize
        StartCoroutine(SpawnGhostWhenReady());
    }

    private System.Collections.IEnumerator SpawnGhostWhenReady()
    {
        // Wait until the DOTS world and our config entity are available
        while (World.DefaultGameObjectInjectionWorld == null || World.DefaultGameObjectInjectionWorld.IsCreated == false)
        {
            yield return null;
        }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Wait until the PedestrianConfig entity from our ConfigSubScene is available
        Entity configEntity = Entity.Null;
        while (configEntity == Entity.Null)
        {
            var query = entityManager.CreateEntityQuery(typeof(PedestrianConfig));
            if (!query.IsEmpty)
            {
                configEntity = query.GetSingletonEntity();
            }
            yield return null; // Wait a frame and check again
        }

        // Now we can safely spawn the ghost
        PedestrianConfig config = entityManager.GetComponentData<PedestrianConfig>(configEntity);
        ghostEntity = entityManager.Instantiate(config.PlayerGhostPrefab);

        // Set Initial Position
        entityManager.SetComponentData(ghostEntity, LocalTransform.FromPositionRotation(
            transform.position,
            transform.rotation
        ));

        Debug.Log("Player ghost entity spawned successfully!");
    }

    private void LateUpdate()
    {
        if (entityManager != null && entityManager.Exists(ghostEntity))
        {
            entityManager.SetComponentData(ghostEntity, LocalTransform.FromPositionRotation(
                transform.position,
                transform.rotation
            ));
        }
    }

    private void OnDestroy()
    {
        if (World.DefaultGameObjectInjectionWorld == null) return;
        if (entityManager != null && entityManager.Exists(ghostEntity))
        {
            entityManager.DestroyEntity(ghostEntity);
        }
    }
}