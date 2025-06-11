using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RayDebugForwardLeftViewer : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRendererObjectForward;
    [SerializeField] private LineRenderer lineRendererObjectLeft;
    [SerializeField] private LineRenderer lineRendererObjectRight;
    [SerializeField] private LineRenderer lineRendererObjectUp;
    [SerializeField] private LineRenderer lineRendererObjectDown;



    [SerializeField] private Color forwardColor = Color.green; // Set color for forward direction
    [SerializeField] private Color leftColor = Color.red;     // Set color for left direction
    [SerializeField] private Color rightColor = Color.blue;   // Set color for left direction
    [SerializeField] private Color upColor = Color.magenta;   // Set color for left direction
    [SerializeField] private Color downColor = Color.white;   // Set color for left direction

    // Length of the rays
    [SerializeField] private float lineLength = 5f;

    // Then, in Update() you can add:

    void Update()
    {
        // Draw forward direction
        RayUtils.DrawLineInVR(lineRendererObjectForward, transform.position, transform.forward, forwardColor, lineLength);

        // Draw left direction (-right is the left direction)
        RayUtils.DrawLineInVR(lineRendererObjectLeft, transform.position, -transform.right, leftColor, lineLength);

        RayUtils.DrawLineInVR(lineRendererObjectRight, transform.position, transform.right, rightColor, lineLength);

        RayUtils.DrawLineInVR(lineRendererObjectUp, transform.position, transform.up, upColor, lineLength);

        RayUtils.DrawLineInVR(lineRendererObjectDown, transform.position, -transform.up, downColor, lineLength);


        // Optional: If you want to add right direction, just add another LineRenderer and method call like this:
        // DrawLineInVR(lineRendererObjectRight, transform.position, transform.right, rightColor, lineLength);
    }

    // Update is called once per frame

   
}
