using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Mathematics;

public class PedestrianSpawner : MonoBehaviour
{
    [SerializeField] GameObject toSpawn;
    [SerializeField] EntityType entityType;
    [SerializeField] Vector2Int currentTile;



    private static int pedestrianNumberToSpawn = 2;
    private static int carNumberToSpawn = 2;


    private Transform tileContainer;
    private bool isActive;

    private float checkInterval = 1.3f; // Check every 1.3 seconds
    private float timeSinceLastCheck = 0f;
    Vector3 worldOffset = Vector3.zero;
    [SerializeField] private bool useDOTSMovement = true;

    private void OnEnable()
    {
        TileManager.OnPlayerTileChanged += HandlePlayerTileChanged;
        WorldRecenterManager.OnWorldRecentered += HandleWorldRecentered;
    }
    public void IncreaseMaxNPCSpawnRate()
    {
        pedestrianNumberToSpawn++;
        carNumberToSpawn++;

    }
    public void DecreaseMaxNPCSpawnRate()
    {
        pedestrianNumberToSpawn--;
        carNumberToSpawn--;
    }
    public void SetGameObjectToSpawn(GameObject objectToSpawn)
    {
        toSpawn = objectToSpawn;
    }
    public void SetEntityType(EntityType ent)
    {
        entityType = ent;
    }
    private void HandleWorldRecentered(Vector3 obj)
    {
        worldOffset -= obj;
    }

    private void OnDestroy()
    {
        TileManager.OnPlayerTileChanged -= HandlePlayerTileChanged;
        WorldRecenterManager.OnWorldRecentered -= HandleWorldRecentered;

    }
    // Start is called before the first frame update
    public void Spawn()
    {
        Debug.Log("Spawn VITO");
        StartCoroutine(SpawnWithWait());
    }
    private IEnumerator SpawnWithWait()
    {
        yield return new WaitForSeconds(3.5f);
        CreateTileContainer();
        SpawnEntities();

        // Perform an initial check
        HandlePlayerTileChanged(TileManager.PlayerOnTile);
    }
    private void FixedUpdate()
    {
        timeSinceLastCheck += Time.fixedDeltaTime;

        // Check periodically based on the interval
        if (timeSinceLastCheck >= checkInterval)
        {
            timeSinceLastCheck = 0f;
            HandlePlayerTileChanged(TileManager.PlayerOnTile);
        }
    }
    public void SpawnEntities()
    {
        int toSpawnNumber = getSpawnNumber();

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // You need to get the Entity version of your Prefab. 
        // You can do this by baking a "Config" object or converting the prefab once.
        // For simplicity, assume you have a reference to the Baked Entity Prefab:
        //todo VITO 
        // Entity pedestrianPrefabEntity = PedestrianConfigAuthoring.Get(entityType);
        // --- FIX STARTS HERE ---

        // 1. Get the Singleton Entity that holds the Config
        // We use a query to find the ONE entity that has the PedestrianConfig component.
        Entity configEntity = Entity.Null;

        try
        {
            // This throws an exception if the entity doesn't exist yet (e.g., SubScene not loaded)
            // So we use a query to check safely.
            var query = entityManager.CreateEntityQuery(typeof(PedestrianConfig));
            if (query.IsEmpty)
            {
                Debug.LogWarning("PedestrianConfig not found! Is the Config SubScene loaded?");
                return;
            }
            configEntity = query.GetSingletonEntity();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error finding PedestrianConfig: {e.Message}");
            return;
        }

        // 2. Read the Component Data
        PedestrianConfig configData = entityManager.GetComponentData<PedestrianConfig>(configEntity);

        // 3. Select the correct prefab based on this Spawner's type
        Entity prefabEntityToSpawn = (entityType == EntityType.Pedestrian)
                                     ? configData.PedestrianPrefab
                                     : configData.CarPrefab;

        // --- FIX ENDS HERE ---
        for (int i = 0; i < toSpawnNumber; i++)
        {
            NodePoint point = PedestrianDestinations.Instance.GetRandomNodePoint(entityType, currentTile);
            if (point == null) continue;

            // Create a Request Entity
            Entity requestEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(requestEntity, new SpawnPedestrianRequest
            {
                PrefabToSpawn = prefabEntityToSpawn,
                Position = point.Position - worldOffset,
                Rotation = quaternion.identity,
                TileIndex = currentTile,
                NpcType = entityType
            });
        }

    }
    private int getSpawnNumber()
    {
        int toSpawnNumber = 0;
        if (entityType.Equals(EntityType.Pedestrian))
        {
            toSpawnNumber = pedestrianNumberToSpawn;
        }
        if (entityType.Equals(EntityType.Car))
        {
            toSpawnNumber = carNumberToSpawn;
        }

        return toSpawnNumber;
    }

    /* //old monobehaviour spawner
    private void SpawnEntities()
    {
        NodePoint lastNode = null;
        int toSpawnNumber = 0;
        if (entityType.Equals(EntityType.Pedestrian))
        {
            toSpawnNumber = pedestrianNumberToSpawn;
        }
        if (entityType.Equals(EntityType.Car))
        {
            toSpawnNumber = carNumberToSpawn;
        }

        for (int i = 0; i < toSpawnNumber; i++)
        {
            NodePoint randomPosition = PedestrianDestinations.Instance.GetRandomNodePoint(entityType, currentTile);
            //  Debug.Log($"VITO Spawned object  {entityType.ToString()}_{i}_{currentTile.x}_{currentTile.y}");

            lastNode = randomPosition;
            //fallback if randomPosition is null or empty
            if (randomPosition == null)
            {
                Debug.Log("VITO SPAWNER FALLBACK1");
                if (lastNode == null)
                    continue;
                randomPosition = lastNode;
                Debug.Log($"VITO USING LAST NODE FALLBACK1 for {entityType.ToString()}_{i}_{currentTile.x}_{currentTile.y}");

            }
            // Get from pool (this activates the object via NpcPoolManager)
            GameObject spawnedObject = entityType == EntityType.Pedestrian
                ? NpcPoolManager.Instance.GetPedestrian()
                : NpcPoolManager.Instance.GetCar();

            //GameObject spawnedObject = NpcPoolManager.Instance.GetPedestrian();
            Pedestrian pedestrianScript = spawnedObject.GetComponent<Pedestrian>();
            pedestrianScript.useDOTSMovement = useDOTSMovement; // Toggle DOTS vs Mono movement
            pedestrianScript.ActivateFromPool(
                randomPosition.Position - worldOffset,
                randomPosition,
                currentTile
            );
        
                            pedestrianScript.SpawnGroup = true;


            //old code
            //      spawnedObject.transform.position = randomPosition.Position - worldOffset;
            spawnedObject.transform.parent = tileContainer;
           // GameObject spawnedObject = Instantiate(toSpawn, randomPosition.Position - worldOffset, Quaternion.identity, tileContainer);
            spawnedObject.name = $"{entityType.ToString()}_{i}_{currentTile.x}_{currentTile.y}";
      //      Pedestrian pedestrianScript = spawnedObject.GetComponent<Pedestrian>();
       //     pedestrianScript.SetTile(currentTile);
         //   pedestrianScript.SetStartingNode(randomPosition);
            //  Debug.Log($"VITO Spawned object's random position: {randomPosition.ToString()}");
        }
    }
    */

    private void CreateTileContainer()
    {
        if (tileContainer != null)
            return;

        string containerName = $"{entityType.ToString()}_Tile_{currentTile.x}_{currentTile.y}";

        // Create a new container GameObject for entity type
        GameObject container = new GameObject(containerName);
        tileContainer = container.transform;

        // Assign the container to the current scene
        SceneManager.MoveGameObjectToScene(container, gameObject.scene);

    }
    private void HandlePlayerTileChanged(Vector2Int playerTile)
    {
        if (tileContainer == null)
        {
            return;
        }
        isActive = playerTile == currentTile;
        if (!isActive)
        {
            ReturnAllNpcsToPool();
            Destroy(tileContainer.gameObject, 4);
        }
        if (isActive && tileContainer.childCount == 0)
        {
            SpawnEntities();
        }
    }
    public static int GetPedestrianNumberToSpawn()
    {
        return pedestrianNumberToSpawn;
    }
    private void ReturnAllNpcsToPool()
    {
        if (tileContainer == null) return;

        // Create a list to avoid modification during iteration
        List<Transform> children = new List<Transform>();
        foreach (Transform child in tileContainer)
        {
            children.Add(child);
        }

        foreach (Transform child in children)
        {
            Pedestrian pedestrian = child.GetComponent<Pedestrian>();
            if (pedestrian != null)
            {
                // REPARENT to NpcPoolManager before releasing
                child.SetParent(NpcPoolManager.Instance.transform);

                if (pedestrian.entityType == EntityType.Pedestrian)
                {
                    NpcPoolManager.Instance.ReleasePedestrian(child.gameObject);
                }
                else if (pedestrian.entityType == EntityType.Car)
                {
                    NpcPoolManager.Instance.ReleaseCar(child.gameObject);
                }
            }
        }

        // Now tileContainer should be empty
        Debug.Log($"Returned {children.Count} NPCs to pool from tile {currentTile}");
    }
}