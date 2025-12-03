using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utils;

public class AStar
{

    public Dictionary<Vector3, NodePoint> Points;


    private readonly object pointsLock = new object();

    private readonly Dictionary<EntityType, List<NodePoint>> bucket
      = new Dictionary<EntityType, List<NodePoint>> {
            { EntityType.Pedestrian, new List<NodePoint>() },
            { EntityType.Car,        new List<NodePoint>() },
          // add other types if needed
      };

    // Tile-specific buckets: tileBuckets[tile][entityType]  List<NodePoint>
    private readonly Dictionary<Vector2Int, Dictionary<EntityType, List<NodePoint>>> tileBuckets
      = new Dictionary<Vector2Int, Dictionary<EntityType, List<NodePoint>>>();

  
    public static readonly float NEIGHBOUR_SEARCH_RADIUS = 100f;

    private List<Vector3> cachedNeighborList = new List<Vector3>();
    public AStar()
    {
        Points = new Dictionary<Vector3, NodePoint>();
    }

    public void AddPoints(List<Vector3> positions, NodeType type, Vector2Int tile)
    {
        lock (pointsLock)
        {
            foreach (Vector3 position in positions)
            {
                if (Points.ContainsKey(position)) continue;


                NodePoint nodePoint = new NodePoint(position, type, tile);
                Points[position] = nodePoint;
                if (type == NodeType.Sidewalk)
                    bucket[EntityType.Pedestrian].Add(nodePoint);
                else if (type == NodeType.Road)
                    bucket[EntityType.Car].Add(nodePoint);

                if (!tileBuckets.TryGetValue(tile, out var tb))
                {
                    tb = new Dictionary<EntityType, List<NodePoint>> {
                        { EntityType.Pedestrian, new List<NodePoint>() },
                        { EntityType.Car,        new List<NodePoint>() }
                    };
                    tileBuckets[tile] = tb;
                }
                if (type == NodeType.Sidewalk)
                    tb[EntityType.Pedestrian].Add(nodePoint);
                else if (type == NodeType.Road)
                    tb[EntityType.Car].Add(nodePoint);

            }
        }
    }
    public List<NodePoint> GetNodesForTile(Vector2Int tile)
    {
        lock (pointsLock)
        {
            if (!tileBuckets.TryGetValue(tile, out var tileBucket))
            {
                // If the tile doesn't exist in our dictionary, return an empty list
                return new List<NodePoint>();
            }

            List<NodePoint> allNodesOnTile = new List<NodePoint>();
            foreach (var nodeList in tileBucket.Values)
            {
                allNodesOnTile.AddRange(nodeList);
            }
            return allNodesOnTile;
        }
    }
    public NodePoint GetRandomPoint(EntityType entityType)
    {
        List<NodePoint> list = bucket[entityType];
        if (list.Count == 0) return null;
        int index = UnityEngine.Random.Range(0, list.Count);
        return list[index];

    }
    public NodePoint GetRandomPoint(EntityType entityType, Vector2Int tile)
    {

        if (tileBuckets.TryGetValue(tile, out var tb))
        {
            var list = tb[entityType];
            if (list != null && list.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, list.Count);
                return list[idx];
            }
        }
        return GetRandomPoint(entityType);

    }

    public NodePoint GetPoint(Vector3 position)
    {
        if (Points.ContainsKey(position))
        {
            return Points[position];
        }
        return null;
    }
    public void AddNeighbour(Vector3 currentPointPosition, Vector3 neighbourPosition)
    {
        lock (pointsLock)
        {


            if (!Points.ContainsKey(currentPointPosition) || !Points.ContainsKey(neighbourPosition))
                return;

            NodePoint currentNode = Points[currentPointPosition];
            NodePoint tobeNeighbourNode = Points[neighbourPosition];
            if (currentNode == tobeNeighbourNode)
                return;

            float distance = Vector3.Distance(currentPointPosition, neighbourPosition);

            Points[currentPointPosition].Neighbours[tobeNeighbourNode] = distance;
            Points[neighbourPosition].Neighbours[currentNode] = distance;
        }
    }
    private void AddNeighboursFromKDTree(Vector3 point, KDTree tree, float radius)
    {
        tree.RadialSearch(point, radius, cachedNeighborList);
        //List<Vector3> neighbours = tree.RadialSearch(point, radius, null);
        foreach (var neighbour in cachedNeighborList)
        {
            AddNeighbour(point, neighbour);
        }
    }
    public void RemovePoints(Vector2Int tile)
    {
        lock (pointsLock)
        {
            if (tileBuckets.TryGetValue(tile, out var tileBucket))
            {
                // Remove from both global and tile buckets
                foreach (var kv in tileBucket)
                {
                    foreach (var np in kv.Value)
                    {
                        Points.Remove(np.Position);
                        bucket[kv.Key].Remove(np);
                    }
                }
                tileBuckets.Remove(tile);
            }
            else
            {
                List<NodePoint> pointsToRemove = new List<NodePoint>();
                foreach (var np in pointsToRemove)
                {
                    Points.Remove(np.Position);
                    if (np.Type == NodeType.Sidewalk)
                        bucket[EntityType.Pedestrian].Remove(np);
                    else if (np.Type == NodeType.Road)
                        bucket[EntityType.Car].Remove(np);
                }

            }
        }
    }
    public void ClearPoints()
    {
        lock (pointsLock)
        {
            if (Points != null)
                Points.Clear();

            //  Clear each global bucket list
            foreach (var list in bucket.Values)
                list.Clear();

            // 3. Clear all per-tile buckets
            tileBuckets.Clear();
        }

    }

    public async Task<List<Vector3>> FindPathAsync(NodePoint start, NodePoint end, KDTree tree, EntityType entityType, CancellationToken token)
    {
//        int maxLenghtOfPath=30; //this could be usefull maybe?
        // Validate input parameters
        if (start == null || end == null)
        {
            Debug.LogError("Start or End Node is null in FindPathAsync!");
            return new List<Vector3>(); // Return an empty path
        }
        // Safety: Ensure start and end nodes are valid (not null) before proceeding to the Task
        if (!Points.ContainsKey(start.Position) || !Points.ContainsKey(end.Position))
        {
            Debug.LogError("Start or End Node is not registered in the AStar graph (Points dictionary).");
            return new List<Vector3>();
        }
        // Create a snapshot of Points.Values to iterate over safely
        NodePoint[] pointsSnapshot = (Points.Values.Where(point => point != null)).ToArray();

        return await Task.Run(() =>
        {
            try
            {
                Dictionary<NodePoint, float> gScore = new Dictionary<NodePoint, float>();
                Dictionary<NodePoint, NodePoint> previous = new Dictionary<NodePoint, NodePoint>();
                HashSet<NodePoint> closedSet = new HashSet<NodePoint>();
                PriorityQueue<NodePoint, float> openSet = new PriorityQueue<NodePoint, float>();
                // Initialize distances to infinity and previous nodes to null
                long maxIterations = long.MaxValue; // Cap search steps to prevent runaway threads
                long currentIterations = 0;

                gScore[start] = 0;

                NodePoint lastProcessedNode = start;

                openSet.Enqueue(start, 0);

                while (openSet.Count > 0)
                {
                    token.ThrowIfCancellationRequested(); // Check for cancellation
                    if (++currentIterations > maxIterations)
                    {
                        Debug.LogWarning($"AStar search exceeded max iterations ({maxIterations}) from {start.Position} to {end.Position}. Returning partial path.");
                        // Path reconstruction on break due to limit
                        // Break the loop and fall through to partial path check below
                        break;
                    }

                    // Find start node with smallest distance
                    openSet.TryDequeue(out NodePoint current, out float currentDistance);
                    if (current == end)
                    {
                        break; // Exit if end node is reached
                    }
                    if (closedSet.Contains(current)) continue; // Check for re-processing

                    closedSet.Add(current);
                    lastProcessedNode = current;

                    foreach (var neighbourPair in current.Neighbours)
                    {
                        NodePoint neighbour = neighbourPair.Key;
                        if (closedSet.Contains(neighbour)) continue;

                        float currentG = gScore[current]; // Guaranteed to exist if current came from openSet
                        float tentativeGScore = currentG + neighbourPair.Value;

                        // FIX: Check neighbor's current G-Score using TryGetValue (Implicit Infinity)
                        float neighborCurrentG = gScore.TryGetValue(neighbour, out float nVal) ? nVal : float.MaxValue;

                        if (tentativeGScore < neighborCurrentG)
                        {
                            previous[neighbour] = current;
                            gScore[neighbour] = tentativeGScore;

                            float heuristicValue = Vector3.Distance(neighbour.Position, end.Position);
                            // FIX: Apply weight to the NEIGHBOR, not the current node (see Recommendation 2)
                            float fScore = (tentativeGScore + heuristicValue) * GetWeightForPoint(neighbour, entityType);

                            openSet.Enqueue(neighbour, fScore);
                        }
                    }

                }


                //                if (previous.ContainsKey(end)) return ReconstructPath(previous, start, end);
                if (previous.ContainsKey(end))
                {
                    List<Vector3> path = ReconstructPath(previous, start, end);
                    if (path != null && path.Count > 1)  // CHANGED: Must have at least 2 points
                    {
                        Debug.Log($"AStar: Found complete path with {path.Count} points");
                        return path;
                    }
                    else if (path != null && path.Count <= 1)
                    {
                        path= ReconstructPath(previous, start, lastProcessedNode);

                        if (path.Count <= 1)
                        {

                        Debug.LogWarning($"AStar: Path has only 1 point. Start and end may be same or unreachable.");
                        // Return empty list to indicate failure
                        return new List<Vector3>();
                        }

                        return path;

                    }
                }

                Debug.Log($"Returning partial path for {start.Position} to {end.Position}, ending at {lastProcessedNode.Position}.");
                    return ReconstructPath(previous, start, lastProcessedNode);
              
                Debug.Log($"AStar failed to find any path for {start.Position} to {end.Position}.");
                return new List<Vector3>();
                /*
                List<Vector3> path = ReconstructPath(previous, start, end);
                if (path.Count <= 0 || (path.Count == 1 && path[0].Equals(start.Position)))
                {
                    path = ReconstructPath(previous, start, lastProcessedNode);
                }

                return path;*/
            }
            catch (OperationCanceledException)
            {
                return new List<Vector3>();
            }
            catch(Exception ex)
            {
                Debug.LogError($"AStar Error: {ex.Message}");
                return new List<Vector3>();
            }
        }, token);
    }

    public List<Vector3> ReconstructPath(Dictionary<NodePoint, NodePoint> previous, NodePoint start, NodePoint end)
    {
        // Check if a path exists once
        if (start != end && !previous.ContainsKey(end)) return null;
        if (start == end)
        {
            return new List<Vector3> { start.Position };
        }
        if (!previous.ContainsKey(end))
        {
            Debug.LogWarning($"ReconstructPath: No path found from {start.Position} to {end.Position}");
            return new List<Vector3> { start.Position };
        }

        List<Vector3> path = new List<Vector3>();
        NodePoint current = end;

        while (current != null && current != start)
        {
            path.Add(current.Position);

            if (previous.TryGetValue(current, out NodePoint prevNode))
            {
                current = prevNode;
            }
        }


            //path.Add(start.Position);
        // Use Array.Reverse for reliability
        Vector3[] pathReversed = path.ToArray();
        System.Array.Reverse(pathReversed, 0, pathReversed.Length);

        // DEBUG: Verify the path makes sense
        if (pathReversed.Length > 0)
        {
            Debug.Log($"ReconstructPath: Created path with {pathReversed.Length} points");
            Debug.Log($"  First (should be start): {pathReversed[0]}");
            Debug.Log($"  Last (should be end): {pathReversed[pathReversed.Length - 1]}");
        }

        return new List<Vector3>(pathReversed);
    }
    private float GetWeightForPoint(NodePoint point, EntityType entityType)
    {
        switch (point.Type)
        {
            case NodeType.Sidewalk:
                if (entityType == EntityType.Car || entityType == EntityType.Tram)
                {
                    return 10000f;
                }
                return 1.0f;
            case NodeType.Road:
                if (entityType == EntityType.Pedestrian)
                {
                    return 30f;
                }
                return 1.0f;
            default:
                return 1;
        }
    }


    public NodePoint GetRandomEndDestination(NodePoint startNode, int depth, EntityType entityType)
    {
        if (startNode == null)
        {
            Debug.LogError("Starting NodePoint is null!");
            return null;
        }

        // Keep track of visited nodes to avoid cycles
        HashSet<NodePoint> visited = new HashSet<NodePoint>();
        NodePoint currentNode = startNode;
        visited.Add(currentNode);
        // Track the current depth
        int currentDepth = 0;

        while (currentDepth < depth)
        {
            if (currentNode.Neighbours.Count == 0)
            {
                return currentNode; // No more neighbors to traverse
            }

            // Process all nodes at the current depth
            List<NodePoint> neighbors = new List<NodePoint>(currentNode.Neighbours.Keys);
            NodePoint randomNeighbor = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
            currentNode = randomNeighbor;

            // Increment depth after processing a level
            currentDepth++;
        }

        return currentNode;

    }
    public void InitializeNeighboursForTile(Vector2Int tile, KDTree tree)
    {
        lock (pointsLock)
        {
            if (!tileBuckets.TryGetValue(tile, out var tileBucket)) return;

            List<NodePoint> allNodesOnTile = new List<NodePoint>();
            foreach (var nodeList in tileBucket.Values)
            {
                allNodesOnTile.AddRange(nodeList);
            }

            if (tree == null) return;

            foreach (var node in allNodesOnTile)
            {
                tree.RadialSearch(node.Position, NEIGHBOUR_SEARCH_RADIUS, cachedNeighborList);
                //  List<Vector3> neighbourPositions = tree.RadialSearch(node.Position, NEIGHBOUR_SEARCH_RADIUS, null);
                foreach (var neighbourPos in cachedNeighborList)
                {
                    AddNeighbour(node.Position, neighbourPos);
                }
            }
        }
    }
}

