using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

using Utils;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    [Header("Player and Raycasting")]
    public Transform player; // Reference to the player object
    public float raycastDistance = 1000f; // Distance for the downward raycast

    private Vector3 worldOffset = Vector3.zero;
    [Header("Tile Configuration")]
    public float loadRadius = 6000; // Radius within which tiles should be considered by distance to be loaded //20000f bakup
    public float unloadRadius = 12000f; // Radius beyond which tiles should be considered by distance to be unloaded //400f bakup
    public Vector2Int gridSize = new Vector2Int(11, 11); // Grid size (e.g., 11x11)
    public string tileScenePrefix = "tile_"; // Prefix for tile scenes (e.g., tile_0_0)

    private const float tileWidth = 10591.83f;
    private const float tileHeight = 10219.93f;
    private static readonly Vector3 startPos = new Vector3(59462, 215, 44170);

    public static event Action<Vector2Int> OnTileUnloaded; // Event to notify listeners about a new tile being destroyed
    public static event Action<Vector2Int> OnPlayerTileChanged;

    public static Vector2Int PlayerOnTile;
    private Vector2Int previousPlayerTile;

    public Dictionary<Vector2Int, bool> loadedTiles = new Dictionary<Vector2Int, bool>();
    private Dictionary<Vector2Int, int> tilePriorities = new Dictionary<Vector2Int, int>();

    private Vector3 tileCenter;
    private string currentTileScene; // Current tile the player is on
    [SerializeField] private int maxOpenScenes = 4;

    // track the offset from the time  it is spawned  and use that for the pedestrian calculation of the path  because of world relocation due to floating origin point.

    public static Dictionary<Vector2Int, Vector3> EntityWorldRecenterOffsets { get; private set; } = new Dictionary<Vector2Int, Vector3>();

    // New dictionary to store the "Receipts" (Handles) for unloading later
    private Dictionary<Vector2Int, AsyncOperationHandle<SceneInstance>> sceneHandles = new Dictionary<Vector2Int, AsyncOperationHandle<SceneInstance>>();

    // Adds or updates the offset for a given tile
    //here for getting values for debugging tools
    public static float TileWidth => tileWidth;  // 10591.83f
    public static float TileHeight => tileHeight; // 10219.93f
    public static Vector3 StartPos => startPos;   // (59462, 215, 44170)
    public void IncreaseMaxOpenScenes()
    {
        maxOpenScenes++;
    }
    public void DecreaseMaxOpenScenes()
    {
        maxOpenScenes--;
    }
    public int GetMaxOpenScenes()
    {
        return maxOpenScenes;
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        // Initialize the tile status dictionary
        for (int x = 0; x <= gridSize.x; x++)
        {
            for (int y = 0; y <= gridSize.y; y++)
            {
                Vector2Int tile = new Vector2Int(x, y);
                loadedTiles[tile] = false;
                tilePriorities[tile] = 0;

            }
        }

        StartCoroutine(CheckTiles());

    }
    public static void SetOffset(Vector3 offset, Vector2Int tile)
    {
        EntityWorldRecenterOffsets[tile] = offset;
        Debug.Log($"VITO Set offset {offset} for tile {tile}");
    }

    public static bool TryGetOffset(Vector2Int tile, out Vector3 offset)
    {
        return EntityWorldRecenterOffsets.TryGetValue(tile, out offset);
    }

    private void OnEnable()
    {
        WorldRecenterManager.OnWorldRecentered += ApplyWorldRecenterOffset;
    }
    private void OnDisable()
    {
        WorldRecenterManager.OnWorldRecentered -= ApplyWorldRecenterOffset;
    }
    private void ApplyWorldRecenterOffset(Vector3 offset)
    {
        this.worldOffset += offset;
        //bug worldOffset = offset;
    }
    [SerializeField] private GameObject spawnDebugCube;
    /*private Vector2 GetTileCenter2D(Vector2Int tile)
    {
        Vector3 totalOffset = WorldRecenterManager.Instance.GetRecenterOffset();
        // Using your established startPos logic:
        float x = startPos.x - totalOffset.x - (tile.x * tileWidth) - (tileWidth * 0.5f);
        float z = startPos.z - totalOffset.z - (tile.y * tileHeight) - (tileHeight * 0.5f);
        return new Vector2(x, z);
    }*/
    private Vector2 GetTileCenter2D(Vector2Int tile, Vector3 currentOffset)
    {
        // Match the Visualizer's definition exactly: the position is effectively the top-right corner of the tile box
        float x = startPos.x - currentOffset.x - (tile.x * tileWidth);
        float z = startPos.z - currentOffset.z - (tile.y * tileHeight);
        return new Vector2(x, z);
    }

    private float DistanceToTile(Vector2 point, Vector2 tileCenter, float width, float height)
    {
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;
        float dx = Mathf.Max(Mathf.Abs(point.x - tileCenter.x) - halfWidth, 0);
        float dz = Mathf.Max(Mathf.Abs(point.y - tileCenter.y) - halfHeight, 0); // note: y represents z here
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
    private float GetEdgeDistanceWithOffset(Vector2 playerPos, Vector2Int tile, Vector3 currentOffset)
    {
        // 1. Calculate Center in current shifted world space
        Vector2 center = GetTileCenter2D(tile, currentOffset);

        // 2. Point-to-AABB Math
        float halfW = tileWidth / 2f;
        float halfH = tileHeight / 2f;

        float dx = Mathf.Max(Mathf.Abs(playerPos.x - center.x) - halfW, 0);
        float dy = Mathf.Max(Mathf.Abs(playerPos.y - center.y) - halfH, 0);

        return Mathf.Sqrt(dx * dx + dy * dy);
    }
    IEnumerator CheckTiles()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            UpdatePlayerTile();

            // Prepare data
            Vector2Int playerTileCoords = PlayerOnTile;
            Vector2 playerPos2D = new Vector2(player.position.x, player.position.z);
            worldOffset = WorldRecenterManager.Instance.GetRecenterOffset();

            // --- 1. ALWAYS LOAD PLAYER'S TILE FIRST ---
            if (!loadedTiles[playerTileCoords])
            {
          //      Debug.Log($"CRITICAL: Loading player's tile {playerTileCoords}");
                loadedTiles[playerTileCoords] = true;
                StartCoroutine(LoadTile(playerTileCoords));
            }

            // Count loaded tiles
            int loadedCount = 0;
            foreach (var t in loadedTiles) if (t.Value) loadedCount++;

            // --- 2. BUILD PRIORITY QUEUE WITH EDGE DISTANCE ---
            PriorityQueue<Vector2Int, TilePriority> tilesQueue = new PriorityQueue<Vector2Int, TilePriority>();

            // Consider ALL tiles in the grid
            for (int x = 0; x <= gridSize.x; x++)
            {
                for (int y = 0; y <= gridSize.y; y++)
                {
                    Vector2Int tile = new Vector2Int(x, y);

                    if (loadedTiles[tile]) continue;

                    //THIS COMMENT IS FALSE.
                    // Calculate tile corner (not center!) for DistanceToTile
                    float edgeDistance = GetEdgeDistanceWithOffset(playerPos2D, tile, worldOffset);

                  //  Vector3 tileCorner = startPos - worldOffset - new Vector3(tile.x * tileWidth, player.position.y, tile.y * tileHeight);
                  //  Vector2 tileCorner2D = new Vector2(tileCorner.x, tileCorner.z);

                    // Calculate EDGE distance (distance from player to tile's edge)
                  //  float edgeDistance = DistanceToTile(playerPos2D, tileCorner2D, tileWidth, tileHeight);

                    // Only queue tiles within load radius
                    if (edgeDistance <= loadRadius)
                    {
                        // Calculate priority based on tile position relative to player
                        int priority = CalculateTilePriority(tile, playerTileCoords);
                        TilePriority tilePriority = new TilePriority(priority, edgeDistance);
                        tilesQueue.Enqueue(tile, tilePriority);

                       // Debug.Log($"Tile {tile} queued - Edge Distance: {edgeDistance}, Priority: {priority}");
                    }
                }
            }

            // --- 3. LOAD TILES FROM QUEUE (up to capacity) ---
            while (tilesQueue.Count > 0 && loadedCount < maxOpenScenes)
            {
                Vector2Int tileToLoad = tilesQueue.Dequeue();

                if (!loadedTiles[tileToLoad])
                {
                    loadedTiles[tileToLoad] = true;
                    StartCoroutine(LoadTile(tileToLoad));
                    loadedCount++;
                }
            }

            // --- 4. UNLOAD TILES USING EDGE DISTANCE ---
            List<Vector2Int> tilesToUnload = new List<Vector2Int>();

            foreach (var kvp in loadedTiles)
            {
                if (!kvp.Value) continue;
                if (kvp.Key == playerTileCoords) continue; // Never unload player's tile

                /*  // Calculate tile corner for edge distance
                  Vector3 tileCorner = startPos - worldOffset - new Vector3(kvp.Key.x * tileWidth, player.position.y, kvp.Key.y * tileHeight);
                  Vector2 tileCorner2D = new Vector2(tileCorner.x, tileCorner.z);

                  // Use EDGE distance for unloading decision
                  float edgeDist = DistanceToTile(playerPos2D, tileCorner2D, tileWidth, tileHeight);
                */
                float edgeDist = GetEdgeDistanceWithOffset(playerPos2D, kvp.Key, worldOffset);

                // Unload if edge is beyond unload radius
                if (edgeDist > unloadRadius)
                {
                    tilesToUnload.Add(kvp.Key);
                    Debug.Log($"Marking {kvp.Key} for unload - Edge distance: {edgeDist} > {unloadRadius}");
                }
            }

            // --- 5. ALSO UNLOAD IF OVER CAPACITY (use edge distance for sorting) ---
            if (loadedCount - tilesToUnload.Count > maxOpenScenes)
            {
                List<Vector2Int> unloadCandidates = new List<Vector2Int>();
                foreach (var kvp in loadedTiles)
                {
                    if (kvp.Value && kvp.Key != playerTileCoords && !tilesToUnload.Contains(kvp.Key))
                    {
                        unloadCandidates.Add(kvp.Key);
                    }
                }

                // Sort by edge distance (farthest edge first)
                unloadCandidates.Sort((a, b) => {
                    /*  Vector3 cornerA = startPos - worldOffset - new Vector3(a.x * tileWidth, player.position.y, a.y * tileHeight);
                      Vector3 cornerB = startPos - worldOffset - new Vector3(b.x * tileWidth, player.position.y, b.y * tileHeight);

                      float edgeDistA = DistanceToTile(playerPos2D, new Vector2(cornerA.x, cornerA.z), tileWidth, tileHeight);
                      float edgeDistB = DistanceToTile(playerPos2D, new Vector2(cornerB.x, cornerB.z), tileWidth, tileHeight);
                    */
                    float distA = GetEdgeDistanceWithOffset(playerPos2D, a, worldOffset);
                    float distB = GetEdgeDistanceWithOffset(playerPos2D, b, worldOffset);
                    return distB.CompareTo(distA); // Farthest first
                });

                int excess = (loadedCount - tilesToUnload.Count) - maxOpenScenes;
                for (int i = 0; i < excess && i < unloadCandidates.Count; i++)
                {
                    tilesToUnload.Add(unloadCandidates[i]);
                }
            }

            // --- 6. EXECUTE UNLOADS ---
            foreach (Vector2Int tile in tilesToUnload)
            {
                Debug.Log($"Unloading tile: {tile}");
                loadedTiles[tile] = false;
                StartCoroutine(UnloadTile(tile));
            }

            // Debug: Print current state
            Debug.Log($"Tile state - Loaded: {loadedCount}/{maxOpenScenes}, Player on: {playerTileCoords}");

            yield return new WaitForSeconds(1f);
        }
    }
    private int CalculateTilePriority(Vector2Int tile, Vector2Int playerTile)
{
    // Priority levels:
    // 5 = Player's current tile (handled separately)
    // 4 = Immediate neighbors (up/down/left/right)
    // 3 = Diagonal neighbors
    // 2 = Other tiles within 2-tile radius
    // 1 = All other tiles
    
    if (tile == playerTile) return 5;
    
    // Check if immediate neighbor
    int dx = Mathf.Abs(tile.x - playerTile.x);
    int dy = Mathf.Abs(tile.y - playerTile.y);
    
    if (dx <= 1 && dy <= 1)
    {
        if (dx == 1 && dy == 1) return 4; // Diagonal
        if (dx == 1 || dy == 1) return 4; // Adjacent
    }
    
    // Check 2-tile radius
    if (dx <= 2 && dy <= 2) return 2;
    
    return 1; // Far away
}
    private void printDistancesQueue(PriorityQueue<Vector2Int, float> tilesDistance)
    {
        // Check if the queue is empty first
        if (tilesDistance.Count == 0)
        {
            Debug.Log("tilesQueue: [] (Empty)");
            return;
        }

        // Access the elements via the exposed UnorderedItems property.
        // This allows iteration without modifying the heap (no Dequeue/Enqueue loop needed).

        System.Text.StringBuilder sb = new System.Text.StringBuilder("tilesQueue (Tile: Distance): [");
        bool isFirst = true;

        // Iterate over the elements and their priorities (distances)
        foreach (var item in tilesDistance.UnorderedItems)
        {
            if (!isFirst)
            {
                sb.Append(", ");
            }

            Vector2Int tile = item.Element;
            float distance = item.Priority;

            // Format the output: (x, y): distance
            sb.Append($"({tile.x}, {tile.y}): {distance:F2}");

            isFirst = false;
        }

        sb.Append("]");
        Debug.Log(sb.ToString());
    }

    public void PrintEntityWorldRecenterOffsetsDictionary()
    {
        if (EntityWorldRecenterOffsets == null || EntityWorldRecenterOffsets.Count == 0)
        {
            Debug.Log("EntityWorldRecenterOffsets is empty or null.");
            return;
        }

        Debug.Log("EntityWorldRecenterOffsets Dictionary Contents:");

        foreach (var entry in EntityWorldRecenterOffsets)
        {
            Vector2Int tileKey = entry.Key;
            Vector3 offsetValue = entry.Value;

            Debug.Log($"Tile: ({tileKey.x}, {tileKey.y}) => Offset: ({offsetValue.x}, {offsetValue.y}, {offsetValue.z})");
        }
    }
    private void ManageTileLoading(Vector2Int tile)
    {
        worldOffset = WorldRecenterManager.Instance.GetRecenterOffset();
        tileCenter = startPos - worldOffset - new Vector3(tile.x * tileWidth, player.position.y, tile.y * tileHeight);
        float distance = Vector3.Distance(player.position, tileCenter);

        if (distance < loadRadius && !loadedTiles[tile])
        {
            // yield return LoadTile(tile);
            loadedTiles[tile] = true;
            StartCoroutine(LoadTile(tile));
        }
        else if (distance > unloadRadius && loadedTiles[tile])
        {
            //yield return UnloadTile(tile);
            loadedTiles[tile] = false;
            StartCoroutine(UnloadTile(tile));
        }
    }

    private void UpdatePlayerTile()
    {
        Vector2Int playerTileCoords = GetTileOfPosition(player.position);
        string detectedTileScene = "tile_" + playerTileCoords.y + "_" + playerTileCoords.x;
        //string detectedTileScene = DetectPlayerTile();
        if (!string.IsNullOrEmpty(detectedTileScene) && detectedTileScene != currentTileScene)
        {
            currentTileScene = detectedTileScene;
            PlayerOnTile = playerTileCoords;
            // PlayerOnTile = GetTileCoordinatesFromSceneName(currentTileScene);
            if (PlayerOnTile != previousPlayerTile)
            {
                previousPlayerTile = PlayerOnTile;

                Debug.Log($"[TileManager] Player moved to a new tile: {PlayerOnTile}");

                OnPlayerTileChanged?.Invoke(PlayerOnTile);
            }
        }
    }
    /*
    IEnumerator LoadTile(Vector2Int tile)
    {
        Debug.Log("In load Tile: " + tile.ToString());
        string sceneName = $"{tileScenePrefix}{tile.y}_{tile.x}";
        if (!DoesSceneExist(sceneName))
        {
            Debug.LogWarning($"Scene {sceneName} does not exist.");
            yield break;
        }

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return loadOperation;

        if (loadOperation.isDone)
        {
            loadedTiles[tile] = true;
            Debug.Log($"Loaded {sceneName}");
        }
    }*/
    IEnumerator LoadTile(Vector2Int tile)
    {
        string address = $"{tileScenePrefix}{tile.y}_{tile.x}.unity";
        // Use Addressables for the load
        var loadOperation = Addressables.LoadSceneAsync(address, LoadSceneMode.Additive);
        yield return loadOperation;

        if (loadOperation.Status == AsyncOperationStatus.Succeeded)
        {
            loadedTiles[tile] = true;
            sceneHandles[tile] = loadOperation;
            Debug.Log($"[Addressables] Loaded {address}");

            // NOTE: ObjectOffsetter.Awake() handles the positioning here automatically.
        }
        else
        {
            Debug.LogError($"Failed to load {address}");
            loadedTiles[tile] = false;
        }
    }

    /*
    IEnumerator UnloadTile(Vector2Int tile)
    {
        Debug.Log("In unload Tile: " + tile.ToString());

        // loadedTiles[tile] = false;
        string sceneName = $"{tileScenePrefix}{tile.y}_{tile.x}";
        if (!DoesSceneExist(sceneName))
        {
            Debug.LogWarning($"Scene {sceneName} does not exist.");
            yield break;
        }

        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sceneName);
        yield return unloadOperation;
        loadedTiles[tile] = false;
        OnTileUnloaded?.Invoke(tile);

    }*/
    IEnumerator UnloadTile(Vector2Int tile)
    {
        if (sceneHandles.TryGetValue(tile, out var handle))
        {
            // Addressables Unload actually wipes the textures from RAM
            var unloadOp = Addressables.UnloadSceneAsync(handle);
            yield return unloadOp;

            sceneHandles.Remove(tile);
            OnTileUnloaded?.Invoke(tile);
        }
        loadedTiles[tile] = false;
    }

private bool DoesSceneExist(string sceneName)
    {
        return SceneUtility.GetBuildIndexByScenePath(sceneName) != -1;
    }
    public Vector2Int GetTileOfPosition(Vector3 playerPosition)
    {
        // Get the total world offset from the WorldRecenterManager
        Vector3 totalWorldOffset = WorldRecenterManager.Instance.GetRecenterOffset();
        // Account for the world offset
        //BUG CODE 
        //  Vector3 originalWorldPosition = playerPosition + worldOffset;
        //FIXED CODE:
        Vector3 originalWorldPosition = playerPosition + totalWorldOffset;

        // Calculate relative position from the starting position
        float relativeX = (startPos.x - originalWorldPosition.x);
        float relativeZ = (startPos.z - originalWorldPosition.z);

        // Calculate the tile indices (ensure proper flooring)
        int tileX = Mathf.RoundToInt(relativeX / tileWidth);
        int tileY = Mathf.RoundToInt(relativeZ / tileHeight);


        return new Vector2Int(tileX, tileY);
    }
    private string DetectPlayerTile()
    {
        if (Physics.Raycast(player.position, Vector3.down, out RaycastHit hit, raycastDistance))
        {
            // Log hit information for debugging
            //   Debug.Log($"[TileManager] Raycast hit: {hit.collider.gameObject.name}");

            // Retrieve the scene name of the hit object
            Scene hitScene = hit.collider.gameObject.scene;

            if (!string.IsNullOrEmpty(hitScene.name))
            {
                return hitScene.name;
            }
        }

        Debug.LogWarning("[TileManager] Raycast did not hit any valid ground object.");
        return null;
    }

    public Vector2Int GetTileCoordinatesFromSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Scene name is null or empty.");
            return Vector2Int.zero; // Return a default value
        }

        // Ensure the scene name starts with "tile_"
        if (!sceneName.StartsWith("tile_"))
        {
            Debug.LogWarning($"Scene name '{sceneName}' does not follow the expected naming convention.");
            return Vector2Int.zero;
        }

        try
        {
            // Remove the "tile_" prefix
            string coordinatePart = sceneName.Substring(5);

            // Split the remaining part into x and y components
            string[] coordinates = coordinatePart.Split('_');
            if (coordinates.Length != 2)
            {
                Debug.LogWarning($"Scene name '{sceneName}' is not correctly formatted. Expected format: 'tile_y_x'.");
                return Vector2Int.zero;
            }

            // Parse the coordinates (note: y comes first in the naming convention)
            int y = int.Parse(coordinates[0]);
            int x = int.Parse(coordinates[1]);

            return new Vector2Int(x, y);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse scene name '{sceneName}'. Error: {ex.Message}");
            return Vector2Int.zero;
        }
    }

    // Helper class for priority queue to handle both priority level and distance
    private struct TilePriority : IComparable<TilePriority>
    {
        public int PriorityLevel;
        public float Distance;

        public TilePriority(int priorityLevel, float distance)
        {
            PriorityLevel = priorityLevel;
            Distance = distance;
        }

        public int CompareTo(TilePriority other)
        {
            // Higher priority level should come first (negative for descending sort)
            int priorityComparison = other.PriorityLevel.CompareTo(PriorityLevel);
            if (priorityComparison != 0) return priorityComparison;

            // If same priority, closer distance should come first
            return Distance.CompareTo(other.Distance);
        }
    }
}