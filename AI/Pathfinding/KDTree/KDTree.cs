using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KDTree
{
    private KDTreeNode root;
    private readonly int dimensions = 3;

    public KDTree(List<Vector3> points)
    {
        root = BuildTree(points, 0);
    }

    private KDTreeNode BuildTree(List<Vector3> points, int depth)
    {
        if (points == null || points.Count == 0) return null;

        int axis = depth % dimensions;
        points = points.OrderBy(p => p[axis]).ToList();
        int median = points.Count / 2;

        KDTreeNode node = new KDTreeNode(points[median], depth)
        {
            Left = BuildTree(points.GetRange(0, median), depth + 1),
            Right = BuildTree(points.GetRange(median + 1, points.Count - (median + 1)), depth + 1)
        };
        return node;
    }
    public void ClearTree()
    {
        root = null;
    }
    public List<Vector3> RadialSearch(Vector3 target, float radius)
    {
        if (root == null) return new List<Vector3>();
        List<Vector3> results = new List<Vector3>();
        RadialSearch(root, target, radius * radius, 0, results);
        return results;
    }

    private void RadialSearch(KDTreeNode node, Vector3 target,
        float radiusSquared, int depth, List<Vector3> results)
    {
        if (node == null)
            return;
        float distanceSquared = (node.Point - target).sqrMagnitude;
        if (distanceSquared <= radiusSquared)
            results.Add(node.Point);

        int axis = depth % dimensions;
        float diff = target[axis] - node.Point[axis];
        float diffSquared = diff * diff;

        KDTreeNode nearBranch = (diff <= 0) ? node.Left : node.Right;
        KDTreeNode farBranch = (diff <= 0) ? node.Right : node.Left;

        RadialSearch(nearBranch, target, radiusSquared, depth + 1, results);

        if (diffSquared <= radiusSquared)
            RadialSearch(farBranch, target, radiusSquared, depth + 1, results);
    }
}

