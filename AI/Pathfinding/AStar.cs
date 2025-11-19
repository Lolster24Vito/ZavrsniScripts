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

    public static readonly float NEIGHBOUR_SEARCH_RADIUS = 50f;

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
        List<Vector3> neighbours = tree.RadialSearch(point, radius);
        foreach (var neighbour in neighbours)
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
        // Validate input parameters
        if (start == null || end == null)
        {
            Debug.LogError("Start or End Node is null in FindPathAsync!");
            return new List<Vector3>(); // Return an empty path
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
                foreach (var node in pointsSnapshot)
                {
                    if (node == null)
                    {
                        Debug.Log(" Null NodePoint encountered during pathfinding initialization.");
                        continue; // Skip processing this node
                    }
                    gScore[node] = float.MaxValue;
                    previous[node] = null;
                }

                gScore[start] = 0;
                // Priority queue (min-heap) to store the nodes that are explored
                //last processed node is used as a fallback path if no path found 
                NodePoint lastProcessedNode = null;

                openSet.Enqueue(start, 0);

                while (openSet.Count > 0)
                {
                    token.ThrowIfCancellationRequested(); // Check for cancellation


                    // Find start node with smallest distance
                    openSet.TryDequeue(out NodePoint current, out float currentDistance);
                    if (current == end)
                    {
                        break; // Exit if end node is reached
                    }
                    closedSet.Add(current);

                    foreach (var neighbourPair in current.Neighbours)
                    {
                        NodePoint neighbour = neighbourPair.Key;
                        if (closedSet.Contains(neighbour))
                        {
                            continue; // Skip this neighbour  it was already evaluated

                        }
                        float calculatedGScore = gScore[current] + neighbourPair.Value;
                        if (calculatedGScore < gScore[neighbourPair.Key])
                        {
                            previous[neighbour] = current;
                            gScore[neighbour] = calculatedGScore;
                            float heuristicValue = Vector3.Distance(neighbour.Position, end.Position);
                            float fScore = (calculatedGScore + heuristicValue) * GetWeightForPoint(current, entityType);
                            lastProcessedNode = current;

                            //a*
                            openSet.Enqueue(neighbourPair.Key, fScore);
                        }
                    }

                }




                List<Vector3> path = ReconstructPath(previous, start, end);
                if (path.Count <= 0 || (path.Count == 1 && path[0].Equals(start.Position)))
                {
                    path = ReconstructPath(previous, start, lastProcessedNode);
                }

                return path;
            }
            catch (OperationCanceledException)
            {
                return new List<Vector3>();
            }
        }, token);
    }

    public List<Vector3> ReconstructPath(Dictionary<NodePoint, NodePoint> previous, NodePoint start, NodePoint end)
    {
        List<Vector3> path = new List<Vector3>();
        NodePoint current = end;

        while (current != null)
        {

            if (!previous.ContainsKey(end))
            {
                Debug.LogError("No path exists to the end node.");
                return new List<Vector3>(); // Return an empty path
            }

            if (previous.ContainsKey(current))
            {
                current = previous[current];
                if (current != null)
                    path.Add(current.Position);

            }
            else
            {
                break;
            }


        }
        if (current == null)
        {
            path.Add(start.Position);
        }

        path.Reverse();
        return path;
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
                List<Vector3> neighbourPositions = tree.RadialSearch(node.Position, NEIGHBOUR_SEARCH_RADIUS);
                foreach (var neighbourPos in neighbourPositions)
                {
                    AddNeighbour(node.Position, neighbourPos);
                }
            }
        }
    }
}


