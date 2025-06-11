using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLeftRightHandWithMiddleRay : MonoBehaviour
{
    // Transforms representing the left and right hand aiming origins,
    // and a separate transform from where the average ray will be drawn.
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform rightHandTransform;
    [SerializeField] private Transform averageTransform;

    // Colors for the lines.
    [SerializeField] private Color leftHandColor = Color.magenta;
    [SerializeField] private Color rightHandColor = Color.magenta;
    [SerializeField] private Color averageColor = Color.green;

    // Length of each visualized ray.
    [SerializeField] private float lineLength = 10f;

    // Internal LineRenderer components.
    private LineRenderer leftHandLine;
    private LineRenderer rightHandLine;
    private LineRenderer averageLine;

    private void Start()
    {
        // Automatically create LineRenderers for each direction.
        leftHandLine = InitializeLineRenderer("LeftHandLine", leftHandColor);
        rightHandLine = InitializeLineRenderer("RightHandLine", rightHandColor);
        averageLine = InitializeLineRenderer("AverageLine", averageColor);
    }

    private void Update()
    {
        // Ensure that all required transforms are assigned.
        if (leftHandTransform == null || rightHandTransform == null || averageTransform == null)
            return;

        // Get the aiming directions from each hand using transform.up.
        Vector3 leftHandAimingDirection = leftHandTransform.up;
        Vector3 rightHandAimingDirection = rightHandTransform.up;

        // Normalize the left and right directions.
        Vector3 leftHandLocalDirection = leftHandAimingDirection.normalized;
        Vector3 rightHandLocalDirection = rightHandAimingDirection.normalized;

        // Compute the average direction.
        // (This matches the code snippet: averageDirection = (leftHandLocalDirection + rightHandLocalDirection) / 2f)
        Vector3 averageDirection = (leftHandLocalDirection + rightHandLocalDirection) / 2f;
        averageDirection = averageDirection.normalized; // Normalize to ensure a consistent ray length

        // Draw the lines using our helper method.
        DrawLine(leftHandLine, leftHandTransform.position, leftHandLocalDirection);
        DrawLine(rightHandLine, rightHandTransform.position, rightHandLocalDirection);
        DrawLine(averageLine, averageTransform.position, averageDirection);
    }

    // Creates a new GameObject with a LineRenderer component, sets its parent, and configures its properties.
    private LineRenderer InitializeLineRenderer(string name, Color color)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.parent = transform;
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();

        // Set a simple material; the "Sprites/Default" shader works well for colored lines.
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        return lr;
    }

    // Updates the given LineRenderer to draw a line from 'start' in the 'direction' scaled by lineLength.
    private void DrawLine(LineRenderer lr, Vector3 start, Vector3 direction)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, start + direction.normalized * lineLength);
    }

}
