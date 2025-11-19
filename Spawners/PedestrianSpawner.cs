using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PedestrianSpawner : MonoBehaviour
{
    [SerializeField] GameObject toSpawn;
    [SerializeField] EntityType entityType;
    [SerializeField] Vector2Int currentTile;



    private static int pedestrianNumberToSpawn = 20;
    private static int carNumberToSpawn = 20;


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
            Destroy(tileContainer.gameObject,4);
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