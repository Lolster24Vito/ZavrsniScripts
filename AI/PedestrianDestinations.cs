using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Threading;

//This is a where all the points from MeshToWorldPoints will be stored
public class PedestrianDestinations : MonoBehaviour
{

    private static PedestrianDestinations instance;
    public static PedestrianDestinations Instance { get { return instance; } }
    public AStar aStar;
    public KDTree tree;


    [SerializeField] private float maxDistanceForNeighbours = 50f;
    private double firstInterval;
    private double lastInterval;
    private bool isPathFindingReady = false;

    private List<Vector3> cachedNeighborList = new List<Vector3>();
    internal void ClearPoints()
    {
        if(aStar!=null)
        aStar.ClearPoints();
        if(tree!=null)
        tree.ClearTree();
    }

    private CancellationTokenSource tokenSource;


    public bool IsPathFindingReady()
    {
        return isPathFindingReady;
    }
    public void SetPathFindingReady(bool ready)
    {
        isPathFindingReady = ready;
        Debug.Log($"Pathfinding system readiness set to: {ready}");
    }


    private void Awake()
    {
        aStar = new AStar();
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }
    private void OnEnable()
    {
        TileManager.OnTileUnloaded += RemovePointsOnTile;
    }

    public void RemovePointsOnTile(Vector2Int tile)
    {
        aStar.RemovePoints(tile);
        UpdateGlobalKDTree();
    }

    private void OnDisable()
    {
        TileManager.OnTileUnloaded -= RemovePointsOnTile;
    }
    private void OnDestroy()
    {
        // Ensure cancellation when the object is destroyed
        tokenSource?.Cancel();
    }

    private void Start()
    {
        firstInterval = Time.realtimeSinceStartupAsDouble;
        Debug.Log("FirstTime:" + firstInterval);
        Debug.Log(aStar.Points.Count);
        /*
       //when debugging I cant see in visual studio any points in aStar but with smaller amount of points I can
      for (int i = 0; i < 1000; i++)
       {
           dijkstra.AddPoint(allSidePoints[i], NodeType.Sidewalk);
       }
       */


        //   InitializeNeighbours();

        //int iSpawner = 0;

        //debug to see neighbours and it's points

        /*
        for (int i = 0; i < 200; i++)
        {
            KeyValuePair<Vector3, NodePoint> keyValuePair = dijkstra.Points.ElementAt(i);
            GameObject mainPoint = Instantiate(mainCube, keyValuePair.Key, Quaternion.identity, transform);
            mainPoint.name = "Main point (" + i + ")";
            for (int j = 0; j < keyValuePair.Value.Neighbours.Count; j++)
            {
                GameObject neighbourPoint = Instantiate(neighbourCube, keyValuePair.Value.Neighbours.ElementAt(j).Key.Position, Quaternion.identity, transform);
                dijkstra.GetPoint(keyValuePair.Value.Neighbours.ElementAt(j).Key.Position);
                neighbourPoint.name = "Neighbour point (" + i + ")____N(" + j + ") ";
                //yield return null;
            }
        }
        lastInterval= Time.realtimeSinceStartupAsDouble;
        Debug.Log("DONE now it is:" + lastInterval);
        Debug.Log("It takes this amount of time" + (lastInterval-firstInterval));
        */

    }
    public void InitializeNeighbours()
    {
        tokenSource?.Cancel();
        // Create a new CancellationTokenSource
        tokenSource = new CancellationTokenSource();

       InitializeNeighboursAsync(tokenSource.Token);
    }
    private async void InitializeNeighboursAsync(CancellationToken token)
    {
        isPathFindingReady = false;
        firstInterval = Time.realtimeSinceStartupAsDouble;
        Debug.Log("VITO FirstTime:" + firstInterval);
        Debug.Log(aStar.Points.Count);
        List<Vector3> points = new List<Vector3>(aStar.Points.Keys);
        //run in background thread
        await Task.Run(() =>
        {
            InitializeNeighborsTask(points, token);
        });

        //return to main thread

        lastInterval = Time.realtimeSinceStartupAsDouble;
        Debug.Log("VITOMIR DONE now it is:" + lastInterval);
        Debug.Log(aStar.Points.Count);
        Debug.Log("It takes this amount of time" + (lastInterval - firstInterval));
        isPathFindingReady = true;
    }

    private void InitializeNeighborsTask(List<Vector3> points, CancellationToken token)
    {
        tree = new KDTree(points);
        for (int i = 0; i < points.Count; i++)
        {
            // Check for cancellation
            if (token.IsCancellationRequested)
            {
                return;
            }
            Vector3 point = points[i];
           tree.RadialSearch(point, maxDistanceForNeighbours, cachedNeighborList);
            foreach (var neighborPoint in cachedNeighborList)
            {
                if (point != neighborPoint)
                {
                    lock (aStar)
                    {
                        aStar.AddNeighbour(point, neighborPoint);
                    }
                }
            }

        }
    }

    public NodePoint GetPoint(Vector3 position)
    {
        return aStar.GetPoint(position);
    }

    public void AddPoints(List<Vector3> points, NodeType type, Vector2Int tile)
    {
        aStar.AddPoints(points, type, tile);
    }


    public List<Vector3> GetNeighboursRadialSearch(Vector3 point)
    {
         tree.RadialSearch(point, maxDistanceForNeighbours, cachedNeighborList);
        return cachedNeighborList;
    }
    public List<Vector3> GetNeighboursRadialSearch(Vector3 point, float distanceNeighbour)
    {
         tree.RadialSearch(point, distanceNeighbour, cachedNeighborList);
        return cachedNeighborList;
    }


    public NodePoint GetRandomNodePoint(EntityType entityType)
    {
        NodePoint randomPoint = aStar.GetRandomPoint(entityType);
        // Fallback to the 7th point if no random point is found
        if (randomPoint == null)
        {
            Debug.LogWarning($"No random point found for entity type {entityType}. Falling back to the 7th point.");

            int indexToRetrieve = 6; // 7th point (index 6)
            if (aStar.Points.Count > indexToRetrieve)
            {
                randomPoint = aStar.Points.ElementAt(indexToRetrieve).Value;
                Debug.Log($"Fallback to the 7th point: {randomPoint.Position}");
            }
            else if (aStar.Points.Count > 0)
            {
                // Fallback to the first point if the 7th point isn't available
                randomPoint = aStar.Points.ElementAt(0).Value;
                Debug.Log($"Fallback to the first point: {randomPoint.Position}");
            }
            else
            {
                // No points at all
                Debug.LogError("No points available in Dijkstra. Returning null.");
                return null;
            }
        }

        return randomPoint;
    }
    Vector2Int defaultTileValue = new Vector2Int(-1, -1);
    public NodePoint GetRandomNodePoint(EntityType entityType, Vector2Int tile)
    {
        if (tile.Equals(defaultTileValue))
            return GetRandomNodePoint(entityType);
        return aStar.GetRandomPoint(entityType, tile);
    }

    public  List<Vector3> FindPathSync(NodePoint start, NodePoint end, EntityType entityType)
    {
        return aStar.FindPathSync(start, end, entityType);
    }

    public async Task<List<Vector3>> FindPathAsync(NodePoint start, NodePoint end, EntityType entityType, CancellationToken token)
    {
        return await aStar.FindPathAsync(start, end, tree, entityType, token);
    }


    public void UpdateGlobalKDTree()
    {
        List<Vector3> allPoints = new List<Vector3>(aStar.Points.Keys);
        if (allPoints.Count == 0)
        {
            tree = null; // No points left, clear the tree
            Debug.Log("KDTree cleared as there are no more points.");
        }
        else
        {
            tree = new KDTree(allPoints);
            Debug.Log($"Global KDTree rebuilt with {allPoints.Count} total points.");
        }
    }
    public NodePoint GetNearestValidNode(Vector3 position, EntityType type, Vector2Int tile)
    {
        // 1. Try exact match (Fastest, but rare for a ragdoll)
        NodePoint precisePoint = GetPoint(position);
        if (IsValidNodeForType(precisePoint, type))
        {
            return precisePoint;
        }

        // 2. Radial Search using KDTree (Finds nearest neighbor)
        // We search within 10 units. 100f from your old code is very large, 
        // we want them to respawn reasonably close to where they fell.
        float searchRadius = 50f;

        // Safety check if tree exists
        if (tree != null)
        {

            tree.RadialSearch(position, searchRadius, cachedNeighborList);

            // Sort by distance to find the absolute closest valid point
            // (RadialSearch doesn't guarantee sorted order)
            cachedNeighborList.Sort((a, b) => Vector3.SqrMagnitude(a - position).CompareTo(Vector3.SqrMagnitude(b - position)));

            foreach (Vector3 neighborPos in cachedNeighborList)
            {
                NodePoint node = GetPoint(neighborPos);

                if (IsValidNodeForType(node, type))
                {
                    return node;
                }
            }
        }

        // 3. Fallback: Get a random point on the tile
        // If they fell into the void or too far from a path, just put them somewhere safe on the tile.
        Debug.LogWarning($"Could not find nearest neighbor for {type} at {position}. Teleporting to random node.");
        return GetRandomNodePoint(type, tile);
    }

    // Helper to check if a node matches the entity type (Pedestrians -> Sidewalks, Cars -> Roads)
    private bool IsValidNodeForType(NodePoint node, EntityType type)
    {
        if (node == null) return false;

        if (type == EntityType.Pedestrian && node.Type == NodeType.Sidewalk) return true;
        if (type == EntityType.Car && node.Type == NodeType.Road) return true; // Fixed logic here

        return false;
    }

}
