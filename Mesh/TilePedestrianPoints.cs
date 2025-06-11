using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilePedestrianPoints : MonoBehaviour
{
    [SerializeField] private List<MeshToWorldPoints> meshToWorldPoints;
    [SerializeField] private List<PedestrianSpawner> spawners;
    [SerializeField] private Vector2Int tile;
    private bool spawned = false;

    void Start()
    {

        TileManager.OnPlayerTileChanged += TileManager_OnPlayerTileChanged;
    }
    private void OnDestroy()
    {
        TileManager.OnPlayerTileChanged -= TileManager_OnPlayerTileChanged;

    }

    private void TileManager_OnPlayerTileChanged(Vector2Int tileChangedTo)
    {
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
