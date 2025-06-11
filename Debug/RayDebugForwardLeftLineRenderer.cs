using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayDebugForwardLeftLineRenderer : MonoBehaviour
{
    private LineRenderer lineRendererForward;
    private LineRenderer lineRendererLeft;
    private LineRenderer lineRendererRight;
    private LineRenderer lineRendererUp;
    private LineRenderer lineRendererDown;

    [SerializeField] private Color forwardColor = Color.green;
    [SerializeField] private Color leftColor = Color.red;
    [SerializeField] private Color rightColor = Color.blue;
    [SerializeField] private Color upColor = Color.magenta;
    [SerializeField] private Color downColor = Color.white;

    [SerializeField] private float lineLength = 5f;

    private void Start()
    {
        // Initialize LineRenderers
        lineRendererForward = InitializeLineRenderer(forwardColor);
        lineRendererLeft = InitializeLineRenderer(leftColor);
        lineRendererRight = InitializeLineRenderer(rightColor);
        lineRendererUp = InitializeLineRenderer(upColor);
        lineRendererDown = InitializeLineRenderer(downColor);
    }

    private void Update()
    {
        // Draw lines in each direction
        DrawLine(lineRendererForward, transform.position, transform.forward);
        DrawLine(lineRendererLeft, transform.position, -transform.right);
        DrawLine(lineRendererRight, transform.position, transform.right);
        DrawLine(lineRendererUp, transform.position, transform.up);
        DrawLine(lineRendererDown, transform.position, -transform.up);
    }

    private LineRenderer InitializeLineRenderer(Color color)
    {
        // Create a new GameObject with LineRenderer
        GameObject lineObject = new GameObject("LineRenderer_" + color);
        lineObject.transform.parent = transform;
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        // Configure LineRenderer
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        return lineRenderer;
    }

    private void DrawLine(LineRenderer lineRenderer, Vector3 start, Vector3 direction)
    {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, start + direction.normalized * lineLength);
    }
}
