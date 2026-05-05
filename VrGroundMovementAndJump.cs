using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VrGroundMovementAndJump : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] public float walkSpeed = 3.5f;
    [SerializeField] private float jumpForce = 6.0f;
    [SerializeField] private int maxJumps = 2; // Allows for that "double jump" test

    [Header("References")]
    [SerializeField] private GlideStateMachineBodyPoses glideState;
    [SerializeField] private Transform cameraRigTransform; // CenterEyeAnchor / Main Camera

    private Rigidbody rb;
    private int currentJumpCount = 0;
    private bool wasOnFloorLastFrame;

    [Header("Input")]
    [SerializeField] private OVRInput.RawAxis2D moveAxis = OVRInput.RawAxis2D.LThumbstick;
    [SerializeField] private OVRInput.RawButton jumpButton = OVRInput.RawButton.A;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Detect landing to reset jumps
        // We check 'isOnFloor' via a public getter or variable in your Glide script
        if (glideState.isOnFloor && !wasOnFloorLastFrame)
        {
            currentJumpCount = 0;
        }
        wasOnFloorLastFrame = glideState.isOnFloor;

        HandleJumpInput();
    }

    void FixedUpdate()
    {
        // Only allow joystick movement if the player is actually grounded
        if (glideState.isOnFloor)
        {
            ApplyGroundMovement();
        }
    }

    private void ApplyGroundMovement()
    {
        Vector2 stick = OVRInput.Get(moveAxis);
        if (stick.magnitude < 0.1f)
        {
            // Stop horizontal sliding instantly when stick is released on ground
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }

        // Head-relative movement direction
        Vector3 forward = cameraRigTransform.forward;
        Vector3 right = cameraRigTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = (forward * stick.y + right * stick.x).normalized;

        // Set velocity directly, preserving any vertical motion
        rb.velocity = new Vector3(moveDir.x * walkSpeed, rb.velocity.y, moveDir.z * walkSpeed);
    }

    private void HandleJumpInput()
    {
        bool jumpPressed = OVRInput.GetDown(jumpButton); // true only on the frame the button is pressed

        if (jumpPressed && currentJumpCount < maxJumps)
        {
            ExecuteJump();
        }
    }

    private void ExecuteJump()
    {
        currentJumpCount++;

        // Reset vertical velocity for consistent jump height
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        // Crucial: Manually trigger the exit floor state so flight mechanics take over immediately
        glideState.OnExitCollisionWithFloor();
    }

}
