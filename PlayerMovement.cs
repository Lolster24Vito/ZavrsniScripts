using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;

    public float groundDrag;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    // Additional variable for godmode speed
    public float godModeMoveSpeed;
    public float godModeFlySpeed;


    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode godModeKey = KeyCode.G;
    public KeyCode ascendKey = KeyCode.LeftShift;
    public KeyCode descendKey = KeyCode.LeftControl;

    public Transform orientation;
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private bool readyToJump;
    private bool godMode = false; 

    private Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;

    }

    // Update is called once per frame
    void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);
        GetInputs();
        LimitSpeedMax();
        if (grounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }

    }
    private void FixedUpdate()
    {
        if(!godMode)
        MovePlayer();
        else
        {
            MovePlayerGodMode();
        }
    }
    private void GetInputs()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        if(Input.GetKey(jumpKey)&&readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(godModeKey))
        {
            ToggleGodMode();
        }

        if (Input.GetKey(KeyCode.Alpha1))
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }
        if (Input.GetKey(KeyCode.Alpha2))
        {
            SceneManager.LoadScene(1, LoadSceneMode.Single);
        }
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

    }
    private void MovePlayer()
    {
        //calc mov direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {//in air
            rb.AddForce(moveDirection.normalized * moveSpeed*airMultiplier * 10f, ForceMode.Force);

        }
    }

    private void LimitSpeedMax()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if (!godMode)
        {
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }

        }
        else
        {
            if (flatVel.magnitude > godModeMoveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * godModeMoveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }

    }
    private void Jump()
    {
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
    }

    private void ToggleGodMode()
    {
        godMode = !godMode;
        if (godMode)
        {
            // If entering godmode, reset velocity and disable gravity
            rb.velocity = Vector3.zero;
            rb.useGravity = false;
        }
        else
        {
            // If exiting godmode, enable gravity
            rb.useGravity = true;
        }
    }
    private void MovePlayerGodMode()
    {
        // Get input for godmode movement
        float godModeHorizontalInput = Input.GetAxisRaw("Horizontal");
        float godModeVerticalInput = Input.GetAxisRaw("Vertical");

        // Calculate movement direction
        Vector3 godModeMoveDirection = orientation.forward * godModeVerticalInput + orientation.right * godModeHorizontalInput;

        // Apply force for godmode movement
        rb.AddForce(godModeMoveDirection.normalized * godModeMoveSpeed * 10f, ForceMode.Force);

        // Check for vertical movement (up and down)
        if (Input.GetKey(KeyCode.LeftShift))
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            // Ascend
            rb.AddForce(transform.up * godModeFlySpeed , ForceMode.Impulse);
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            // Descend
            rb.AddForce(-transform.up * godModeFlySpeed , ForceMode.Impulse);
        }

    }
}
