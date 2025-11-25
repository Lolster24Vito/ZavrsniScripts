using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;

public class PlayerDOTSBridge : MonoBehaviour
{
    private Entity ghostEntity = Entity.Null;
    private EntityManager entityManager;

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        SpawnGhost();
        WorldRecenterManager.OnWorldRecentered += ApplyRecenterOffset;

    }

    private void OnDestroy()
    {
        WorldRecenterManager.OnWorldRecentered -= ApplyRecenterOffset;

        // If it's null, the game is quitting, and the entity is already destroyed anyway.
        if (World.DefaultGameObjectInjectionWorld == null) return;

        // Cleanup: Don't leave a ghost floating forever if the player quits/dies
        if (entityManager.Exists(ghostEntity))
        {
            entityManager.DestroyEntity(ghostEntity);
        }
    }

    private void LateUpdate()
    {
        // Sync the Real Player's position to the DOTS Ghost
        if (entityManager.Exists(ghostEntity))
        {
            entityManager.SetComponentData(ghostEntity, LocalTransform.FromPositionRotation(
                transform.position,
                transform.rotation
            ));
        }
    }

    private void SpawnGhost()
    {
        // 1. Get the Config
        Entity configEntity = Entity.Null;
        try
        {
            var query = entityManager.CreateEntityQuery(typeof(PedestrianConfig));
            if (!query.IsEmpty)
            {
                configEntity = query.GetSingletonEntity();
            }
        }
        catch { }

        if (configEntity == Entity.Null)
        {
            Debug.LogError("PlayerBridge: Could not find PedestrianConfig! Is the SubScene loaded?");
            return;
        }

        // 2. Instantiate the Ghost
        PedestrianConfig config = entityManager.GetComponentData<PedestrianConfig>(configEntity);
        ghostEntity = entityManager.Instantiate(config.PlayerGhostPrefab);

        // 3. Set Initial Position
        entityManager.SetComponentData(ghostEntity, LocalTransform.FromPositionRotation(
            transform.position,
            transform.rotation
        ));
        //todo vito ovo mozda sve zezne
        // 4. IMPORTANT: Make sure the ghost entity has physics components
        if (!entityManager.HasComponent<PhysicsVelocity>(ghostEntity))
        {
            entityManager.AddComponentData(ghostEntity, new PhysicsVelocity());
        }
    }
    private void ApplyRecenterOffset(Vector3 offset)
    {
        if (entityManager.Exists(ghostEntity))
        {
            var transform = entityManager.GetComponentData<LocalTransform>(ghostEntity);
            transform.Position -= (float3)offset;
            entityManager.SetComponentData(ghostEntity, transform);
        }
    }
}