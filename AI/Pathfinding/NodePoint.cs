using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//IEquality is mainly used for tracking if it's the same position, Icomparer for the Neighbours's distance for the HashSet in aStar  specifically   PriorityQueue<NodePoint, float> openSet
public class NodePoint : IEqualityComparer<Vector3>, IComparer<NodePoint>
{
    public Vector3 Position { get; set; }
    //Dictionary with the nodes neighbours and it's distance to the neighbours
    public Dictionary<NodePoint, float> Neighbours { get; set; }
    public NodeType Type { get; set; }
    //tile is used for getting random points on tiles
    public Vector2Int Tile { get; set; }


    public NodePoint(Vector3 position, NodeType type, Vector2Int tile)
    {
        Position = position;
        Type = type;
        Neighbours = new Dictionary<NodePoint, float>();
        Tile = tile;
    }

    public bool Equals(Vector3 x, Vector3 y)
    {
        return x.Equals(y);
    }

    public int GetHashCode(Vector3 obj)
    {
        // Generate a hash code based on the position
        return Position.GetHashCode();
    }

    // Implementation of IComparer<NodePoint> interface
    public int Compare(NodePoint a, NodePoint b)
    {
        // Compare the positions of the NodePoints
        int result = a.Position.x.CompareTo(b.Position.x);
        if (result == 0)
        {
            result = a.Position.y.CompareTo(b.Position.y);
            if (result == 0)
            {
                // Optional: Compare z-coordinate if needed
                result = a.Position.z.CompareTo(b.Position.z);
            }
        }
        return result;
    }

    public override string ToString()
    {
        return $"position:{Position}, Tile:{Tile.ToString()},Type: {Type}";
    }
}
