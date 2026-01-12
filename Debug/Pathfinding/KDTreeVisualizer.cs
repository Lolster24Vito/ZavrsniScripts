#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

public class KDTreeVisualizer : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showKDTreePoints = true;
    [SerializeField] private bool showNodeConnections = false;

    [Header("Point Display Limits")]
    [SerializeField] private int maxRoadPointsToShow = 200;
    [SerializeField] private int maxSidewalkPointsToShow = 200;
    [SerializeField] private float pointSize = 0.5f;

    private PedestrianDestinations pedestrianDestinations;

    private Dictionary<NodeType, int> maxPointsPerType = new Dictionary<NodeType, int>();

    // NEW: Counters to track how many points of each type are drawn
    private int roadsDrawn = 0;
    private int sidewalksDrawn = 0;

    private void Awake()
    {
        maxPointsPerType[NodeType.Road] = maxRoadPointsToShow;
        maxPointsPerType[NodeType.Sidewalk] = maxSidewalkPointsToShow;

        // Use singleton pattern if available, with a safe fallback
        if (PedestrianDestinations.Instance != null)
        {
            pedestrianDestinations = PedestrianDestinations.Instance;
        }
        else
        {
            pedestrianDestinations = FindObjectOfType<PedestrianDestinations>();
            if (pedestrianDestinations == null)
            {
                Debug.LogWarning("KDTreeVisualizer: No PedestrianDestinations found in scene.");
                enabled = false; // Disable script if component not found
                return;
            }
        }

        // Check if AStar and its Points are initialized
        if (pedestrianDestinations.aStar == null || pedestrianDestinations.aStar.Points == null)
        {
            Debug.LogWarning("KDTreeVisualizer: AStar or Points not initialized. Disabling.");
            enabled = false;
            return;
        }
    }

    private void OnDrawGizmos()
    {
        // Draw only if toggled on and components are available
        if (!showKDTreePoints || pedestrianDestinations?.aStar?.Points == null) return;

        // Reset counters for this frame
        roadsDrawn = 0;
        sidewalksDrawn = 0;

        int count = 0;
        int totalPoints = pedestrianDestinations.aStar.Points.Count;

        // CORRECTED: Use a more efficient loop with early break
        foreach (var point in pedestrianDestinations.aStar.Points)
        {
            // CORRECTED: Get the max points to show for this node's type
            int maxPointsToShow = (point.Value.Type == NodeType.Road) ?
                maxRoadPointsToShow : maxSidewalkPointsToShow;

            // Skip drawing if we've reached the limit for this type
            if (count >= maxPointsToShow) break;
            if (point.Value == null) continue; // CORRECTED: Added null check for safety

            // Use color coding based on node type
            switch (point.Value.Type)
            {
                case NodeType.Sidewalk:
                    Gizmos.color = Color.green;
                    sidewalksDrawn++; // CORRECTED: Increment sidewalk counter
                    break;
                case NodeType.Road:
                    Gizmos.color = Color.red;
                    roadsDrawn++; // CORRECTED: Increment road counter
                    break;
                default:
                    Gizmos.color = Color.white;
                    break;
            }

            Gizmos.DrawSphere(point.Key, pointSize);
            count++;
        }

        // CORRECTED: Display info about what was drawn
        if (count < totalPoints && totalPoints > 0)
        {
            Vector3 infoPos = Camera.current != null ? Camera.current.transform.position : Vector3.zero;
            UnityEditor.Handles.Label(infoPos + Vector3.up * 10f,
                $"Showing {count}/{totalPoints} points ({roadsDrawn} roads, {sidewalksDrawn} sidewalks)");
        }
    }

    private void DrawNodeConnections()
    {
        if (pedestrianDestinations?.aStar?.Points == null) return;

        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f); // Light blue for connections

        int count = 0;
        foreach (var point in pedestrianDestinations.aStar.Points)
        {
            if (count >= maxRoadPointsToShow) break; // Use road limit for connections too
            if (point.Value == null || point.Value.Neighbours == null) continue;

            Vector3 from = point.Key;

            foreach (var neighbor in point.Value.Neighbours)
            {
                if (neighbor.Key == null) continue;
                Vector3 to = neighbor.Key.Position;
                Gizmos.DrawLine(from, to);
            }

            count++;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showKDTreePoints || pedestrianDestinations?.aStar?.Points == null) return;

        // Draw all KDTree points
        if (showKDTreePoints) DrawKDTreePoints();

        // Draw connections between nodes
        if (showNodeConnections) DrawNodeConnections();
    }
    private void DrawKDTreePoints()
    {
        int count = 0;
        int totalPoints = pedestrianDestinations.aStar.Points.Count;

        foreach (var point in pedestrianDestinations.aStar.Points)
        {
            // Get the max points to show for this specific node type
            int maxPointsToShow = maxPointsPerType.ContainsKey(point.Value.Type) ?
                maxPointsPerType[point.Value.Type] : 200;

            // Skip drawing if we've reached the limit for this type
            if (count >= maxPointsToShow) break;
            if (point.Value == null) continue;

            // Use color coding based on node type
            switch (point.Value.Type)
            {
                case NodeType.Sidewalk:
                    Gizmos.color = Color.green;
                    sidewalksDrawn++; // Increment sidewalk counter
                    break;
                case NodeType.Road:
                    Gizmos.color = Color.red;
                    roadsDrawn++; // Increment road counter
                    break;
                default:
                    Gizmos.color = Color.white;
                    break;
            }

            Gizmos.DrawSphere(point.Key, pointSize);
            count++;
        }

        // Display info about what was drawn
        if (count < totalPoints && totalPoints > 0)
        {
            Vector3 infoPos = Camera.current != null ? Camera.current.transform.position : Vector3.zero;
            UnityEditor.Handles.Label(infoPos + Vector3.up * 10f,
                $"Showing {count}/{totalPoints} points ({roadsDrawn} roads, {sidewalksDrawn} sidewalks)");
        }
    }
    // Context Menu items for easy toggling
    [ContextMenu("Toggle KDTree Points")]
    private void ToggleKDTreePoints()
    {
        showKDTreePoints = !showKDTreePoints;
        Debug.Log($"KDTree points visualization: {showKDTreePoints}");
    }

    [ContextMenu("Toggle Node Connections")]
    private void ToggleNodeConnections()
    {
        showNodeConnections = !showNodeConnections;
        Debug.Log($"Node connections visualization: {showNodeConnections}");
    }

    [ContextMenu("Set Max Road Points")]
    private void SetMaxRoadPointsToShow()
    {
        maxRoadPointsToShow = Mathf.Max(1, maxRoadPointsToShow); // Ensure at least 1
        Debug.Log($"Max road points to show: {maxRoadPointsToShow}");
    }

    [ContextMenu("Set Max Sidewalk Points")]
    private void SetMaxSidewalkPointsToShow()
    {
        maxSidewalkPointsToShow = Mathf.Max(1, maxSidewalkPointsToShow); // Ensure at least 1
        Debug.Log($"Max sidewalk points to show: {maxSidewalkPointsToShow}");
    }
}
#endif