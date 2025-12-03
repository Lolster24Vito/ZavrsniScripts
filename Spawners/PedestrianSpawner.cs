using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PedestrianSpawner : MonoBehaviour
{
    [SerializeField] private int maxAgents = 15; // Total pool size for this spawner
    [SerializeField] GameObject toSpawn;
    [SerializeField] EntityType entityType;
    [SerializeField] Vector2Int currentTile;

    [Header("Bubble Settings")]
     private float spawnRadius = 500;
     private float despawnRadius = 1200f;
    private float minSpawnDistance = 10f;

    private static int pedestrianNumberToSpawn = 3;
    private static int carNumberToSpawn = 3;


    private Transform tileContainer;
    private bool isActive;

    private float checkInterval = 1.3f; // Check every 1.3 seconds
    private float timeSinceLastCheck = 0f;
    Vector3 worldOffset = Vector3.zero;
    [SerializeField] private bool useDOTSMovement = true;

    private List<GameObject> activeAgents = new List<GameObject>();
    private Transform playerTransform;

    private void OnEnable()
    {
        TileManager.OnPlayerTileChanged += HandlePlayerTileChanged;
        WorldRecenterManager.OnWorldRecentered += HandleWorldRecentered;
    }
    private void Start()
    {
        if (TileManager.Instance != null && TileManager.Instance.player != null)
        {
            playerTransform = TileManager.Instance.player;
        }
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
    private void Update()
    {
        // Check distances and reposition agents outside despawn radius
        for (int i = 0; i < activeAgents.Count; i++)
        {
            GameObject agent = activeAgents[i];
            if (agent == null || !agent.activeSelf) continue;

            float distSq = (playerTransform.position - agent.transform.position).sqrMagnitude;

            if (distSq > despawnRadius * despawnRadius)
            {
                RespawnAgentNearPlayer(agent);
            }
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
    public void ActivateSpawner(Vector2Int tile)
    {
        currentTile = tile;

        // Initial spawn
        for (int i = 0; i < maxAgents; i++)
        {
            SpawnAgent();
        }
    }

    // Called by TilePedestrianPoints when tile becomes inactive
    public void DeactivateSpawner()
    {
        ReturnAllNpcsToPool();
    }

    private void SpawnAgent()
    {
        // Get from pool
        GameObject agent = entityType == EntityType.Pedestrian
            ? NpcPoolManager.Instance.GetPedestrian()
            : NpcPoolManager.Instance.GetCar();

        activeAgents.Add(agent);
        RespawnAgentNearPlayer(agent); // Position it correctly immediately
    }

    private void RespawnAgentNearPlayer(GameObject agent)
    {
        if (playerTransform == null || agent == null) return;

        // 1. Find valid points in the "Donut" (between min and max radius)
        Vector3 playerPos = playerTransform.position;
        Vector3 spawnPosition = FindValidSpawnPosition(playerPos);

        if (spawnPosition == Vector3.zero)
        {
            // Couldn't find valid position - release back to pool
            ReturnAgentToPool(agent);
            return;
        }

        // 2. Reset Agent Logic
        Pedestrian pedScript = agent.GetComponent<Pedestrian>();
        if (pedScript != null)
        {
            // Get node point at spawn position
            NodePoint startNode = PedestrianDestinations.Instance.GetPoint(spawnPosition);
            if (startNode == null)
            {
                // Fallback: find nearest node point
                startNode = GetNearestNodePoint(spawnPosition);
            }

            if (startNode != null)
            {
                // Use your existing activation method
                pedScript.ActivateFromPool(
                    spawnPosition,
                    startNode,
                    currentTile
                );

                // Optional: Face away from player for more natural spawning
                Vector3 awayFromPlayer = (spawnPosition - playerPos).normalized;
                if (awayFromPlayer != Vector3.zero)
                {
                    agent.transform.rotation = Quaternion.LookRotation(awayFromPlayer);
                }

                agent.transform.parent = tileContainer;
            }
            else
            {
                Debug.LogWarning($"Could not find node point at position {spawnPosition}");
                ReturnAgentToPool(agent);
            }
        }
    }
    private Vector3 FindValidSpawnPosition(Vector3 playerPos)
    {
        // Try multiple times to find a good spawn position
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // Get random direction and distance within spawn radius
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
            float distance = Random.Range(minSpawnDistance, spawnRadius);

            Vector3 candidatePos = playerPos + (randomDirection * distance);

            // Check if position is on valid terrain and not too steep
            if (IsValidSpawnLocation(candidatePos))
            {
                // Get nearest node point to ensure it's on a valid path
                NodePoint nearestNode = GetNearestNodePoint(candidatePos);
                if (nearestNode != null)
                {
                    return nearestNode.Position;
                }
            }
        }

        // Fallback: Use KDTree radial search
        List<Vector3> nearbyPoints = new List<Vector3>();
        if (PedestrianDestinations.Instance != null && PedestrianDestinations.Instance.tree != null)
        {
            PedestrianDestinations.Instance.tree.RadialSearch(playerPos, spawnRadius, nearbyPoints);

            foreach (Vector3 point in nearbyPoints)
            {
                float dist = Vector3.Distance(playerPos, point);
                if (dist > minSpawnDistance && dist < spawnRadius)
                {
                    return point;
                }
            }
        }

        return Vector3.zero;
    }

    private NodePoint GetNearestNodePoint(Vector3 position)
    {
        // Use your existing KDTree to find nearest node point
        List<Vector3> nearbyPoints = new List<Vector3>();
        if (PedestrianDestinations.Instance != null && PedestrianDestinations.Instance.tree != null)
        {
            // Search in a small radius
            PedestrianDestinations.Instance.tree.RadialSearch(position, 10f, nearbyPoints);

            if (nearbyPoints.Count > 0)
            {
                // Find closest point
                Vector3 closest = nearbyPoints[0];
                float closestDist = Vector3.Distance(position, closest);

                for (int i = 1; i < nearbyPoints.Count; i++)
                {
                    float dist = Vector3.Distance(position, nearbyPoints[i]);
                    if (dist < closestDist)
                    {
                        closest = nearbyPoints[i];
                        closestDist = dist;
                    }
                }

                return PedestrianDestinations.Instance.GetPoint(closest);
            }
        }

        // Fallback: get random node point from current tile
        return PedestrianDestinations.Instance.GetRandomNodePoint(entityType, currentTile);
    }

    private bool IsValidSpawnLocation(Vector3 position)
    {
        // Add terrain checks here if needed
        // Example: Check if position is on walkable surface
        // You can use Physics.Raycast to check for ground and slope

        Ray ray = new Ray(position + Vector3.up * 10f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f))
        {
            // Check slope
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope < 45f) // Max 45 degree slope
            {
                return true;
            }
        }

        return false;
    }

    private void ReturnAgentToPool(GameObject agent)
    {
        if (agent == null) return;

        activeAgents.Remove(agent);
        Pedestrian pedestrian = agent.GetComponent<Pedestrian>();

        if (pedestrian != null)
        {
            if (pedestrian.entityType == EntityType.Pedestrian)
            {
                NpcPoolManager.Instance.ReleasePedestrian(agent);
            }
            else if (pedestrian.entityType == EntityType.Car)
            {
                NpcPoolManager.Instance.ReleaseCar(agent);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerTransform.position, spawnRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, despawnRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistance);
        }
    }
}