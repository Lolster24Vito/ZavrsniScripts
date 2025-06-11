using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayDebugForwardLeftGizmoViewer : MonoBehaviour
{
    [SerializeField] private Color forwardColor = Color.green;
    [SerializeField] private Color leftColor = Color.red;
    [SerializeField] private Color rightColor = Color.blue;
    [SerializeField] private Color upColor = Color.magenta;
    [SerializeField] private Color downColor = Color.white;

    [SerializeField] private float lineLength = 5f;

    private void OnDrawGizmos()
    {
        // Forward direction
        DrawGizmoLine(transform.position, transform.forward, forwardColor);

        // Left direction
        DrawGizmoLine(transform.position, -transform.right, leftColor);

        // Right direction
        DrawGizmoLine(transform.position, transform.right, rightColor);

        // Up direction
        DrawGizmoLine(transform.position, transform.up, upColor);

        // Down direction
        DrawGizmoLine(transform.position, -transform.up, downColor);
    }

    private void DrawGizmoLine(Vector3 start, Vector3 direction, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(start, start + direction.normalized * lineLength);
    }
}
