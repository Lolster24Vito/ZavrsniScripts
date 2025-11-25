using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class TileManager : MonoBehaviour
{
    [Header("Player and Raycasting")]
    public Transform player; // Reference to the player object
    public float raycastDistance = 1000f; // Distance for the downward raycast

    private Vector3 worldOffset = Vector3.zero;
    [Header("Tile Configuration")]
    public float loadRadius = 20000f; // Radius within which tiles should be loaded
    public float unloadRadius = 40000f; // Radius beyond which tiles should be unloaded
    public Vector2Int gridSize = new Vector2Int(11, 11); // Grid size (e.g., 11x11)
    public string tileScenePrefix = "tile_"; // Prefix for tile scenes (e.g., tile_0_0)

    private const float tileWidth = 10591.83f;
    private const float tileHeight = 10219.93f;
    private readonly Vector3 startPos = new Vector3(59462, 215, 44170);

    public static event Action<Vector2Int> OnTileUnloaded; // Event to notify listeners about a new tile being destroyed
    public static event Action<Vector2Int> OnPlayerTileChanged;

    public static Vector2Int PlayerOnTile;
    private Vector2Int previousPlayerTile;

    private Dictionary<Vector2Int, bool> loadedTiles = new Dictionary<Vector2Int, bool>();
    private Dictionary<Vector2Int, int> tilePriorities = new Dictionary<Vector2Int, int>();

    private Vector3 tileCenter;
    private string currentTileScene; // Current tile the player is on
    [SerializeField] private int maxOpenScenes = 4;
    [SerializeField] private int criticalPerformanceThreshold = 1; // Below this, only player tile is kept
    // track the offset from the time  it is spawned  and use that for the pedestrian calculation of the path  because of world relocation due to floating origin point.

    public static Dictionary<Vector2Int, Vector3> EntityWorldRecenterOffsets { get; private set; } = new Dictionary<Vector2Int, Vector3>();
  
    private float halfTileWidth;
    private float halfTileHeight;
    private float sqrLoadRadius;
    private float sqrUnloadRadius; // Pre-calculated squared radius

   // public Unity.Scenes.SceneHandle loadedSubSceneHandle; //THIS IS WHAT GEMINI SUGGESTED IT's OUTDATED WITH MY 1.0 or newer dots.

    public void IncreaseMaxOpenScenes()
    {
        maxOpenScenes++;
    }
    public void DecreaseMaxOpenScenes()
    {
        maxOpenScenes = Mathf.Max(criticalPerformanceThreshold, maxOpenScenes - 1);
    }
    public int GetMaxOpenScenes()
    {
        return maxOpenScenes;
    }

    void Start()
    {
        // Calculate these ONCE at startup
        halfTileWidth = tileWidth * 0.5f;
        halfTileHeight = tileHeight * 0.5f;

        sqrLoadRadius = loadRadius * loadRadius;
        // Calculate squared radius once so we don't do (radius * radius) in the loop
        sqrUnloadRadius = unloadRadius * unloadRadius;

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
    bool spawnedDebugCubeOnce = false;
    private float DistanceToTile(Vector2 point, Vector2 tileCenter)
    {
        // Calculate distance from center to point
        float dx = Mathf.Abs(point.x - tileCenter.x);
        float dy = Mathf.Abs(point.y - tileCenter.y); // point.y is actually Z in world space

        // Subtract half-width to get distance from edge (clamped to 0 if inside)
        // We do the subtraction first, then Max, to avoid extra branching
        dx = Mathf.Max(dx - halfTileWidth, 0f);
        dy = Mathf.Max(dy - halfTileHeight, 0f);

        // Return squared Euclidean distance (a^2 + b^2)
        return dx * dx + dy * dy;
    }
    private Vector3 GetTileCenterWorldPosition(Vector2Int tileCoords)
    {
        // Calculate the center of the tile relative to (0,0,0) of startPos, then apply offset
        Vector3 localTileCenter = new Vector3(
            startPos.x - (tileCoords.x * tileWidth) - halfTileWidth,
            player.position.y, // Keep the player's Y for distance calculation if tiles are mostly flat
            startPos.z - (tileCoords.y * tileHeight) - halfTileHeight
        );

        // Return the current world position by adjusting for the recentering offset
        return localTileCenter - WorldRecenterManager.Instance.GetRecenterOffset();
    }

    private float DistanceToTileSq(Vector2 point, Vector2 tileCenter)
    {
        // Calculate distance from center to point
        float dx = Mathf.Abs(point.x - tileCenter.x);
        float dy = Mathf.Abs(point.y - tileCenter.y); // point.y is actually Z in world space

        // Subtract half-width/height to get distance from edge (clamped to 0 if inside)
        dx = Mathf.Max(dx - halfTileWidth, 0f);
        dy = Mathf.Max(dy - halfTileHeight, 0f);

        // Return squared Euclidean distance (a^2 + b^2)
        return dx * dx + dy * dy;
    }

    IEnumerator CheckTiles()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            UpdatePlayerTile();

            Vector2 playerPos2D = new Vector2(player.position.x, player.position.z);
            Vector2Int playerTileCoords = GetTileOfPosition(player.position);

            // 1. Calculate Priorities and populate the queue
            PriorityQueue<Vector2Int, TilePriority> tilesQueue = new PriorityQueue<Vector2Int, TilePriority>();

            // Reset all priorities
            foreach (var tile in loadedTiles.Keys)
            {
                tilePriorities[tile] = 0;
            }

            // Define Priority groups (using the indices 10, 4, 3 from your previous attempt)

            // Priority 10: Player's current tile
            if (tilePriorities.ContainsKey(playerTileCoords)) tilePriorities[playerTileCoords] = 10;

            // Priority 4: Cardinals
            Vector2Int[] cardinals = {
                new Vector2Int(playerTileCoords.x - 1, playerTileCoords.y), new Vector2Int(playerTileCoords.x + 1, playerTileCoords.y),
                new Vector2Int(playerTileCoords.x, playerTileCoords.y - 1), new Vector2Int(playerTileCoords.x, playerTileCoords.y + 1)
            };
            foreach (var tile in cardinals)
            {
                if (tilePriorities.ContainsKey(tile)) tilePriorities[tile] = 4;
            }

            // Priority 3: Diagonals
            Vector2Int[] diagonalTiles = {
                new Vector2Int(playerTileCoords.x - 1, playerTileCoords.y - 1), new Vector2Int(playerTileCoords.x + 1, playerTileCoords.y - 1),
                new Vector2Int(playerTileCoords.x - 1, playerTileCoords.y + 1), new Vector2Int(playerTileCoords.x + 1, playerTileCoords.y + 1)
            };
            foreach (var tile in diagonalTiles)
            {
                if (tilePriorities.ContainsKey(tile)) tilePriorities[tile] = 3;
            }


            // 2. Populate Loading Queue (Crucial Fix)
            // Iterate over ALL possible tiles to check for LOADING candidates.
            foreach (var tile in loadedTiles.Keys)
            {
                // Only consider UNLOADED tiles for loading queue
                if (loadedTiles[tile]) continue;

                Vector3 loopTileCenter = GetTileCenterWorldPosition(tile);
                Vector2 tileCenter2D = new Vector2(loopTileCenter.x, loopTileCenter.z);
                float edgeDistanceSq = DistanceToTileSq(playerPos2D, tileCenter2D);

                // Only enqueue if within the loading radius
                if (edgeDistanceSq <= sqrLoadRadius)
                {
                    TilePriority priority = new TilePriority(tilePriorities[tile], edgeDistanceSq);
                    tilesQueue.Enqueue(tile, priority);
                }
            }

            // 3. Execute Loading
            int loadedCount = 0;
            foreach (var tile in loadedTiles) { if (tile.Value) loadedCount++; }

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

            // 4. Execute Unloading
            List<Vector2Int> tilesToUnloadCandidates = new List<Vector2Int>();

            // Gather all currently loaded tiles, excluding the player's tile
            foreach (var tile in loadedTiles.Keys)
            {
                if (loadedTiles[tile] && tile != playerTileCoords)
                {
                    tilesToUnloadCandidates.Add(tile);
                }
            }

            // Sort tiles: Lowest Priority first, then Furthest Distance first
            tilesToUnloadCandidates.Sort((a, b) =>
            {
                // Compare by Priority (Ascending, so lower priority first)
                int priorityCompare = tilePriorities[a].CompareTo(tilePriorities[b]);
                if (priorityCompare != 0) return priorityCompare;

                // Compare by Distance (Descending, so furthest first)
                Vector3 cA = GetTileCenterWorldPosition(a);
                Vector3 cB = GetTileCenterWorldPosition(b);
                float sqrDistA = Vector3.SqrMagnitude(player.position - cA);
                float sqrDistB = Vector3.SqrMagnitude(player.position - cB);

                return sqrDistB.CompareTo(sqrDistA);
            });

            int tilesUnloaded = 0;
            int numToUnloadDueToLimit = Mathf.Max(0, loadedCount - maxOpenScenes);

            foreach (var tile in tilesToUnloadCandidates)
            {
                Vector3 tileCenter = GetTileCenterWorldPosition(tile);
                float distanceSq = Vector3.SqrMagnitude(player.position - tileCenter);

                bool outOfBounds = distanceSq > sqrUnloadRadius;
                bool neededForLimit = tilesUnloaded < numToUnloadDueToLimit;

                // Unload if out of bounds OR if we need to free up a slot due to the scene limit
                if (outOfBounds || neededForLimit)
                {
                    if (loadedTiles[tile]) // Sanity check
                    {
                        loadedTiles[tile] = false;
                        StartCoroutine(UnloadTile(tile));
                        tilesUnloaded++;
                    }
                }
            }

            yield return new WaitForSeconds(1f); // Delay between checks
        }
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
    }

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