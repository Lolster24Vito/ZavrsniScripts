using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

public class BulletGhost : MonoBehaviour
{
    private Entity ghostEntity = Entity.Null;
    private EntityManager entityManager;
    private bool isInitialized = false;

    private void Awake()
    {
        // Cache the EntityManager reference once
        if (World.DefaultGameObjectInjectionWorld != null)
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
    }

    private void OnEnable()
    {
        if (entityManager == default) return; // World not ready
        SpawnGhost();
    }

    private void OnDisable()
    {
        DestroyGhost();
    }

    private void LateUpdate()
    {
        // Sync position every frame
        if (isInitialized && entityManager.Exists(ghostEntity))
        {
            entityManager.SetComponentData(ghostEntity, LocalTransform.FromPositionRotation(
                transform.position,
                transform.rotation
            ));
        }
    }

    private void SpawnGhost()
    {
        // 1. Find the Config
        // We use a safe query pattern in case the subscene isn't fully loaded yet
        Entity configEntity = Entity.Null;
        try
        {
            var query = entityManager.CreateEntityQuery(typeof(PedestrianConfig));
            if (!query.IsEmpty)
            {
                configEntity = query.GetSingletonEntity();
            }
        }
        catch { return; }

        if (configEntity == Entity.Null) return;

        // 2. Get the Prefab from Config
        PedestrianConfig config = entityManager.GetComponentData<PedestrianConfig>(configEntity);

        // 3. Instantiate
        ghostEntity = entityManager.Instantiate(config.BulletGhostPrefab);

        // 4. Set Initial Position immediately
        entityManager.SetComponentData(ghostEntity, LocalTransform.FromPositionRotation(
            transform.position,
            transform.rotation
        ));

        isInitialized = true;
    }

    private void DestroyGhost()
    {
        // FIX: Check if World exists to prevent errors on App Quit
        if (World.DefaultGameObjectInjectionWorld == null) return;

        if (isInitialized && entityManager.Exists(ghostEntity))
        {
            entityManager.DestroyEntity(ghostEntity);
        }
        ghostEntity = Entity.Null;
        isInitialized = false;
    }
}