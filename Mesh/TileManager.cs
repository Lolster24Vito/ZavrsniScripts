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
    private Vector3 tileCenter;
    private string currentTileScene; // Current tile the player is on
  [SerializeField]  private int maxOpenScenes=4;

    // track the offset from the time  it is spawned  and use that for the pedestrian calculation of the path  because of world relocation due to floating origin point.

    public static Dictionary<Vector2Int, Vector3> EntityWorldRecenterOffsets { get; private set; } = new Dictionary<Vector2Int, Vector3>();

    // Adds or updates the offset for a given tile

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

    void Start()
    {
        // Initialize the tile status dictionary
        for (int x = 0; x <= gridSize.x; x++)
        {
            for (int y = 0; y <= gridSize.y; y++)
            {
                loadedTiles[new Vector2Int(x, y)] = false;
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
    private float DistanceToTile(Vector2 point, Vector2 tileCenter, float width, float height)
    {
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;
        float dx = Mathf.Max(Mathf.Abs(point.x - tileCenter.x) - halfWidth, 0);
        float dz = Mathf.Max(Mathf.Abs(point.y - tileCenter.y) - halfHeight, 0); // note: y represents z here
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
    IEnumerator CheckTiles()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            UpdatePlayerTile();


            // Create a copy of the keys to avoid modifying the dictionary while iterating
            List<Vector2Int> tilesToCheck = new List<Vector2Int>(loadedTiles.Keys);
            Vector2 playerPos2D = new Vector2(player.position.x, player.position.z);
            worldOffset = WorldRecenterManager.Instance.GetRecenterOffset();

            //NEW CODE START
            Vector2Int playerTileCoords = GetTileOfPosition(player.position);
            Debug.Log("VITO DEBUG PLAYER TILE COORDINATES:" + playerTileCoords.ToString());

            // Define priority groups
            List<Vector2Int> priorityTiles = new List<Vector2Int>();

            // First priority: player's tile
            priorityTiles.Add(playerTileCoords);

            // Second priority: adjacent tiles in cardinal directions
            priorityTiles.Add(new Vector2Int(playerTileCoords.x - 1, playerTileCoords.y)); // Left
            priorityTiles.Add(new Vector2Int(playerTileCoords.x + 1, playerTileCoords.y)); // Right
            priorityTiles.Add(new Vector2Int(playerTileCoords.x, playerTileCoords.y - 1)); // Down
            priorityTiles.Add(new Vector2Int(playerTileCoords.x, playerTileCoords.y + 1)); // Up
            PriorityQueue<Vector2Int, float> tilesDistance = new PriorityQueue<Vector2Int, float>();

            foreach (var tile in tilesToCheck)
            {
                if (priorityTiles.Contains(tile)) continue;

                Vector3 loopTileCenter = startPos - worldOffset-new Vector3(tile.x * tileWidth, player.position.y, tile.y * tileHeight);
                Vector2 tileCenter2D = new Vector2(loopTileCenter.x, loopTileCenter.z);
                float edgeDistance = DistanceToTile(playerPos2D, tileCenter2D, tileWidth, tileHeight);
                tilesDistance.Enqueue(tile, edgeDistance);
            }
            spawnedDebugCubeOnce=true;

            Debug.Log("VITO 0 STOp");

           // PrintEntityWorldRecenterOffsetsDictionary();
            printDistancesQueue(tilesDistance);
            // First, ensure priority tiles are loaded
            foreach (var tile in priorityTiles)
            {
                if (loadedTiles.ContainsKey(tile) && !loadedTiles[tile])
                {
                    loadedTiles[tile] = true;
                    StartCoroutine(LoadTile(tile));
                }
            }
            int loadedCount = 0;
            foreach (var tile in loadedTiles)
            {
                if (tile.Value)
                {
                    loadedCount++;
                }
            }

            while (tilesDistance.Count > 0 && loadedCount < maxOpenScenes)
            {
                Vector2Int tileToLoad = tilesDistance.Dequeue();

                //load if not loaded
                if ( !loadedTiles[tileToLoad])
                {
                    loadedTiles[tileToLoad] = true;
                    StartCoroutine(LoadTile(tileToLoad));
                    loadedCount++;
                }
            }
           
            //new code: 30_10

            // Then, check all loaded tiles to see if any should be unloaded
            List<Vector2Int> tilesToUnload = new List<Vector2Int>();
            foreach (var tile in loadedTiles)
            {
                // Skip if it's a priority tile
                if (priorityTiles.Contains(tile.Key)) continue;

                // Skip if it's already being unloaded
                if (!tile.Value) continue;

                // Calculate distance to this tile
                Vector3 tileCenter = startPos - worldOffset - new Vector3(tile.Key.x * tileWidth, player.position.y, tile.Key.y * tileHeight);
                float distance = Vector3.Distance(player.position, tileCenter);

                // If beyond unload radius, mark for unloading
                if (distance > unloadRadius)
                {
                    tilesToUnload.Add(tile.Key);
                }
            }

            // Unload marked tiles
            foreach (var tile in tilesToUnload)
            {
                loadedTiles[tile] = false;
                StartCoroutine(UnloadTile(tile));
                loadedCount--;
            }
            Debug.Log("VITO 1 STOp");
           //  PrintEntityWorldRecenterOffsetsDictionary();

            yield return new WaitForSeconds(1f); // Delay between checks to reduce CPU usage
        }
    }

    private void printDistancesQueue(PriorityQueue<Vector2Int, float> tilesDistance)
    {
        Debug.Log("tilesQueue" + tilesDistance.ToString());
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
}