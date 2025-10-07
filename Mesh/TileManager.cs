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
        worldOffset = offset;
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
            PriorityQueue<Vector2Int, float> tilesDistance=new PriorityQueue<Vector2Int, float>();
            Vector2 playerPos2D = new Vector2(player.position.x, player.position.z);
            worldOffset = WorldRecenterManager.Instance.GetRecenterOffset();

            foreach (var tile in tilesToCheck)
            {
                //old code

                //offsetWithoutFirst = WorldRecenterManager.Instance.GetCustomWorldOffsetWithoutFirst();

                Vector3 loopTileCenter = startPos - worldOffset-new Vector3(tile.x * tileWidth, player.position.y, tile.y * tileHeight);
                //new code this 124 line is skipped
                //   float distance = Vector3.Distance(player.position, loopTileCenter);
                // Use the tile's x/z as a Vector2.
                Vector2 tileCenter2D = new Vector2(loopTileCenter.x, loopTileCenter.z);
                // Instead of center-to-center distance, compute the distance from the player to the tile's boundaries.
                float edgeDistance = DistanceToTile(playerPos2D, tileCenter2D, tileWidth, tileHeight);
                // !!! CHANGE IS HERE !!! Use edgeDistance for sorting
                // If the player is inside the tile, edgeDistance will be 0, prioritizing it correctly.
                tilesDistance.Enqueue(tile, edgeDistance);

                //old code with old unneded distance that only factored in center
             //   tilesDistance.Enqueue(tile, distance);
            //    Debug.Log("tiledistances for:" + tile.ToString() + " ,positionOfTile:"+loopTileCenter.ToString() +"distance:" + distance);
                //ManageTileLoading(tile);
              /*  if (!spawnedDebugCubeOnce)
                {
                    GameObject debugCube = Instantiate(spawnDebugCube, loopTileCenter, Quaternion.identity);
                    debugCube.name = "Cube tile(" + tile.y + "," + tile.x + ")";
                }*/
            }
            spawnedDebugCubeOnce=true;
            //PrintEntityWorldRecenterOffsetsDictionary();
            //printDistancesQueue(tilesDistance);

            for (int i = 0; i < maxOpenScenes && tilesDistance.Count > 0 ; i++)
            {
                Vector2Int tileToLoad = tilesDistance.Dequeue();

                //load if not loaded
                if ( !loadedTiles[tileToLoad])
                {
                    // yield return LoadTile(tile);
                    loadedTiles[tileToLoad] = true;
                    StartCoroutine(LoadTile(tileToLoad));

                }
            }
            //else for unload if not present
            while (tilesDistance.Count > 0)
            {
                Vector2Int tileToLoad = tilesDistance.Dequeue();
                if (loadedTiles[tileToLoad])
                {
                    //yield return UnloadTile(tile);
                    loadedTiles[tileToLoad] = false;
                    StartCoroutine(UnloadTile(tileToLoad));
                }
            }
           // PrintEntityWorldRecenterOffsetsDictionary();

            /*
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
        } */
            //PrintEntityWorldRecenterOffsetsDictionary();
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
        string detectedTileScene = DetectPlayerTile();
        if (!string.IsNullOrEmpty(detectedTileScene) && detectedTileScene != currentTileScene)
        {
            currentTileScene = detectedTileScene;
            PlayerOnTile = GetTileCoordinatesFromSceneName(currentTileScene);
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
        // Account for the world offset
        Vector3 adjustedPlayerPosition = playerPosition + worldOffset;
        // Calculate relative position from the starting position
        float relativeX = adjustedPlayerPosition.x - startPos.x;
        float relativeZ = adjustedPlayerPosition.z - startPos.z;

        // Calculate the tile indices (ensure proper flooring)
        int tileX = Mathf.FloorToInt(relativeX / tileWidth);
        int tileY = Mathf.FloorToInt(relativeZ / tileHeight);

        // Debugging logs to verify correctness
        Debug.Log($"[TileManager] Player Position: {playerPosition}, Adjusted Position: {adjustedPlayerPosition}, World Offset: {worldOffset}");
        Debug.Log($"[TileManager] Relative Position: ({relativeX}, {relativeZ}), Tile Indices: ({tileX}, {tileY})");

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