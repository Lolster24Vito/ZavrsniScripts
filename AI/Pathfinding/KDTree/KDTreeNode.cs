using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KDTreeNode
{
    public Vector3 Point { get; set; }
    public int Depth { get; set; }
    public KDTreeNode Left { get; set; }
    public KDTreeNode Right { get; set; }

    public KDTreeNode(Vector3 point, int depth)
    {
        Point = point;
        Depth = depth;
        Left = null;
        Right = null;
    }




}
