using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class DotsSpawnerBridge : MonoBehaviour
{
    public static DotsSpawnerBridge Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void RequestSpawn(EntityType type, Vector2Int tile, Vector3 position)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // 1. Get the config singleton to know WHICH prefab to spawn
        // We need to do this via a query because we are in Mono-land
        Entity query = entityManager.CreateEntityQuery(typeof(PedestrianConfig)).GetSingletonEntity();
        PedestrianConfig config = entityManager.GetComponentData<PedestrianConfig>(query);

        Entity prefabToUse = (type == EntityType.Pedestrian) ? config.PedestrianPrefab : config.CarPrefab;

        // 2. Create the Request Entity
        Entity requestEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(requestEntity, new SpawnPedestrianRequest
        {
            PrefabToSpawn = prefabToUse,
            Position = position,
            Rotation = quaternion.identity,
            TileIndex = tile,
            NpcType = type
        });
    }
}