using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RayUtils
{

    public static void DrawLineInVR(LineRenderer lineRenderer, Vector3 start, Vector3 direction, Color color,
                             float lineLength = 10f, float width = 0.01f)
    {
        // Set LineRenderer properties
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.positionCount = 2;

        // Set start and end positions (extend the direction vector by lineLength)
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, start + direction.normalized * lineLength);
        lineRenderer.useWorldSpace = true;

        // Optional: If needed, you can still control when the line disappears, but no need to destroy the object
        // Example: Disable the LineRenderer after some time (e.g., using a coroutine)
    }
}
