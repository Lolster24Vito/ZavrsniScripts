using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshToWorldPoints : MonoBehaviour
{
    [SerializeField] private NodeType type;
    [SerializeField] private Vector2Int tile;

    private MeshFilter meshFilter;

    private List<Vector3> worldPoints = new List<Vector3>();
    private Vector3 recenterWorldOffset = Vector3.zero;

    public Vector2Int Tile { get => tile; set => tile = value; }
    public NodeType Type { get => type; set => type = value; }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    public void CalculateWorldPoints()
    {

        TileManager.SetOffset(WorldRecenterManager.Instance.GetCustomWorldOffsetWithoutFirst(), tile);
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("MeshFilter or mesh not found.");
            return;
        }
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        worldPoints.Clear();
        int sampleRate = 2; // Adjust this based on your needs

        // Transform vertices to world space
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            Vector3 worldPoint = transform.TransformPoint(vertex);
            worldPoints.Add(worldPoint);

            if (i % 2 == 1 && i < (vertices.Length - 1))
            {
                Vector3 previousVertex = vertices[i - 1];

                Vector3 middlePoint = (worldPoint + transform.TransformPoint(previousVertex)) / 2f;
                worldPoints.Add(middlePoint);
            }
        }
        if (PedestrianDestinations.Instance != null)
        {
            PedestrianDestinations.Instance.AddPoints(worldPoints, type, tile);
        }

    }



}
