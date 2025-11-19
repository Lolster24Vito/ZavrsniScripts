using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class TilePedestrianPoints : MonoBehaviour
{
    private struct NeighbourResult
    {
        public Vector3 Origin;
        public Vector3 Target;

        public NeighbourResult(Vector3 origin, Vector3 target)
        {
            Origin = origin;
            Target = target;
        }
    }

    [SerializeField] private List<MeshToWorldPoints> meshToWorldPoints;
    [SerializeField] private List<PedestrianSpawner> spawners;
    [SerializeField] private Vector2Int tile;
    private bool spawned = false;
    [Header("LOD Configuration")]
    // Radius (in tiles) where ALL geometry is rendered.
    // 0 = only player's tile. 1 = player tile + 1 tile radius.
    public int lod0RadiusTiles = 0;
    private Coroutine initializationCoroutine;
    private const int NODES_PER_FRAME = 40;

    // Keywords to disable rendering for at distant LOD
    private readonly string[] lodTargetKeywords = new string[]
    {
        "osm_roads", "osm_paths", "osm_railways", "osm_water", "osm_areas_pedestrian"
        // Using "osm_roads" covers all its sub-types (motorway, track, tertiary, etc.)
    };

    private List<MeshRenderer> lodRenderers = new List<MeshRenderer>();
    private void Awake()
    {
        //fill lodRenderers list with road,paths,objects
        InitializeLODComponents();
        //turn off road renders for better experience
        foreach (MeshRenderer renderer in lodRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }
    void Start()
    {

        TileManager.OnPlayerTileChanged += TileManager_OnPlayerTileChanged;
        TileManager_OnPlayerTileChanged(TileManager.PlayerOnTile);
    }
    private void OnDestroy()
    {
        TileManager.OnPlayerTileChanged -= TileManager_OnPlayerTileChanged;
        PedestrianDestinations.Instance.RemovePointsOnTile(tile);
    }

    private void TileManager_OnPlayerTileChanged(Vector2Int tileChangedTo)
    {

       // SetLODState(tileChangedTo);

        if (!spawned&& tileChangedTo.Equals(tile))
        {
            if (initializationCoroutine != null)
            {
                StopCoroutine(initializationCoroutine);
            }

            // Start the asynchronous loading process
           InitializeTileDataAsync();
/*
            PedestrianDestinations.Instance.ClearPoints();
            foreach (MeshToWorldPoints m in meshToWorldPoints)
            {
                m.CalculateWorldPoints();
            }
            PedestrianDestinations.Instance.InitializeNeighbours();
          
            StartCoroutine(waitAndSpawn());*/
        }
        if(spawned&& !tileChangedTo.Equals(tile))
        {
            PedestrianDestinations.Instance.RemovePointsOnTile(tile);
            spawned = false;

        }

    }
    private async void InitializeTileDataAsync()
    {
        Debug.Log($"Starting async initialization for tile {tile}.");
        // ---------------------------------------------------------
        // STEP 1: MAIN THREAD - Calculate Points & Prepare Data
        // ---------------------------------------------------------

        // 1. Calculate and add all points for this tile, yielding between each mesh.
        foreach (MeshToWorldPoints m in meshToWorldPoints)
        {
            m.CalculateWorldPoints();
        }
        List<NodePoint> nodesOnThisTile = PedestrianDestinations.Instance.aStar.GetNodesForTile(tile);
        List<Vector3> newPointsToProcess = nodesOnThisTile.Select(n => n.Position).ToList();
        List<Vector3> allWorldPoints = new List<Vector3>(PedestrianDestinations.Instance.aStar.Points.Keys);
        float searchRadius = AStar.NEIGHBOUR_SEARCH_RADIUS;
        // This holds the results so we don't touch the live graph from the thread
        List<NeighbourResult> calculatedResults = new List<NeighbourResult>();
        // 2. Run Heavy Math on Background Thread
        await Task.Run(() =>
        {
            // Rebuild tree only for the snapshot (expensive but off-thread)
            // Or better: Pass the existing tree if your KDTree is thread-safe for reading
            KDTree tempTree = new KDTree(allWorldPoints);

            // Calculate neighbors ONLY for the NEW points
            foreach (Vector3 origin in newPointsToProcess)
            {
                // This heavy math happens in background without freezing game
                List<Vector3> neighbors = tempTree.RadialSearch(origin, searchRadius);
                foreach (Vector3 target in neighbors)
                {
                    // Self-check: don't link to self
                    if (origin != target)
                    {
                        // Add to our results list. Structs are thread-safe.
                        calculatedResults.Add(new NeighbourResult(origin, target));
                    }
                }
                // Store results to apply later (Don't touch aStar here!)
                // You would need a temporary structure to hold these pairs
            }
        });
        // ---------------------------------------------------------
        // STEP 3: MAIN THREAD - Apply Results
        // ---------------------------------------------------------
        Debug.Log($"[Async] Calculation done. Applying {calculatedResults.Count} connections for tile {tile}.");
        var aStar = PedestrianDestinations.Instance.aStar;
        foreach (var result in calculatedResults)
        {
            // AddNeighbour handles the Dictionary lookup and verification internally
            aStar.AddNeighbour(result.Origin, result.Target);
        }

        // Update the global tree for pathfinding queries (optional, but good for consistency)
        PedestrianDestinations.Instance.UpdateGlobalKDTree();



        Debug.Log($"Finished async initialization for tile {tile}.");
        // 4. CRITICAL: Signal to the singleton that pathfinding is now ready.
        if (PedestrianDestinations.Instance != null)
        {
            PedestrianDestinations.Instance.SetPathFindingReady(true);
        }
        Debug.Log($"[Async] Initialization complete for tile {tile}. Spawning pedestrians...");
        spawned = true;
        // 3. Now that data is ready, spawn the pedestrians
        StartCoroutine(waitAndSpawn());
    }

    private IEnumerator InitializeNeighboursForTileAsync()
    {
        // Get the necessary instances from the singleton
        var aStar = PedestrianDestinations.Instance.aStar;
        var tree = PedestrianDestinations.Instance.tree;

        if (aStar == null || tree == null)
        {
            Debug.LogError("AStar or KDTree is not initialized. Cannot initialize neighbours.");
            yield break;
        }

        // Get all the nodes that belong to this specific tile
        List<NodePoint> allNodesOnTile = aStar.GetNodesForTile(tile);
        Debug.Log($"Found {allNodesOnTile.Count} nodes to process for neighbours on tile {tile}.");

        // Process the nodes in chunks
        for (int i = 0; i < allNodesOnTile.Count; i++)
        {
            NodePoint node = allNodesOnTile[i];

            // Find all points within the search radius using the fast KD-Tree
            List<Vector3> neighbourPositions = tree.RadialSearch(node.Position, AStar.NEIGHBOUR_SEARCH_RADIUS);
            foreach (var neighbourPos in neighbourPositions)
            {
                // The AddNeighbour method handles the logic of adding the connection
                // and prevents duplicates.
                aStar.AddNeighbour(node.Position, neighbourPos);
            }

            // After processing a chunk, yield to the next frame to keep the game responsive
            if (i % NODES_PER_FRAME == 0)
            {
                yield return null;
            }
        }
        Debug.Log($"Finished processing neighbours for tile {tile}.");
    }
    private void InitializeLODComponents()
    {
        // Find all MeshRenderers in children recursively, including inactive ones (though tiles are active when loaded)
        MeshRenderer[] allRenderers = GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer mr in allRenderers)
        {
            string objName = mr.gameObject.name.ToLower();
            bool isTarget = false;

            foreach (string keyword in lodTargetKeywords)
            {
                if (objName.Contains(keyword))
                {
                    isTarget = true;
                    break;
                }
            }

            if (isTarget)
            {
                // Crucially, we only manage the MeshRenderer. The GameObject remains active.
                lodRenderers.Add(mr);
            }
        }
    }

    IEnumerator waitAndSpawn()
    {
        yield return new WaitForSeconds(4f);

        spawned = true;
        foreach (PedestrianSpawner spawner in spawners)
        {
            spawner.Spawn();
            yield return new WaitForSeconds(0.35f);

        }
    }
}
