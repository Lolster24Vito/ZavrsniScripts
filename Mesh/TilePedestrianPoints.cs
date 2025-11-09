using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilePedestrianPoints : MonoBehaviour
{
    [SerializeField] private List<MeshToWorldPoints> meshToWorldPoints;
    [SerializeField] private List<PedestrianSpawner> spawners;
    [SerializeField] private Vector2Int tile;
    private bool spawned = false;
    [Header("LOD Configuration")]
    // Radius (in tiles) where ALL geometry is rendered.
    // 0 = only player's tile. 1 = player tile + 1 tile radius.
    public int lod0RadiusTiles = 0;

    // Keywords to disable rendering for at distant LOD
    private readonly string[] lodTargetKeywords = new string[]
    {
        "osm_roads", "osm_paths", "osm_railways", "osm_water", "osm_areas_pedestrian"
        // Using "osm_roads" covers all its sub-types (motorway, track, tertiary, etc.)
    };

    private List<MeshRenderer> lodRenderers = new List<MeshRenderer>();
    private void Awake()
    {
        InitializeLODComponents();
    }
    void Start()
    {

        TileManager.OnPlayerTileChanged += TileManager_OnPlayerTileChanged;
        SetLODState(TileManager.PlayerOnTile);
    }
    private void OnDestroy()
    {
        TileManager.OnPlayerTileChanged -= TileManager_OnPlayerTileChanged;

    }

    private void TileManager_OnPlayerTileChanged(Vector2Int tileChangedTo)
    {
        SetLODState(tileChangedTo);

        if (!spawned&& tileChangedTo.Equals(tile))
        {
            PedestrianDestinations.Instance.ClearPoints();
            foreach (MeshToWorldPoints m in meshToWorldPoints)
            {
                m.CalculateWorldPoints();
            }
            PedestrianDestinations.Instance.InitializeNeighbours();
            StartCoroutine(waitAndSpawn());
        }//todo else remove points if not removed idk.
        if(spawned&& !tileChangedTo.Equals(tile))
        {
            PedestrianDestinations.Instance.RemovePointsOnTile(tile);
            spawned = false;

        }

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
    private void SetLODState(Vector2Int newPlayerTile)
    {
        // Calculate Chebyshev distance (Max of X/Y difference)
        int tileDx = Mathf.Abs(tile.x - newPlayerTile.x);
        int tileDy = Mathf.Abs(tile.y - newPlayerTile.y);
        int tileDistance = Mathf.Max(tileDx, tileDy);

        // LOD 0 (Rendering ON): Player tile or within the radius
        bool isClose = (tileDistance <= lod0RadiusTiles);

        foreach (MeshRenderer renderer in lodRenderers)
        {
            if (renderer != null)
            {
                // If close, enable rendering. If far, disable rendering.
                renderer.enabled = isClose;
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
            yield return new WaitForSeconds(0.3f);

        }
    }
}
