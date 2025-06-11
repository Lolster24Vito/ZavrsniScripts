using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody))]
public class PlayerDebugFlying : MonoBehaviour
{
    [Tooltip("Action to toggle flying state")]
    public InputAction action = null;

    public Transform leftHand;
    public Transform head;
    public float flyingSpeed;

    // Event for starting to fly
    public UnityEvent OnFlyStart = new UnityEvent();

    // Event for stopping flying
    public UnityEvent OnFlyStop = new UnityEvent();
    private Rigidbody rb;

    private bool isFlying;
    private bool hasAlreadyBeenTriggered;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true; // Ensure gravity is on by default
        rb.isKinematic = false; // Use kinematic for direct position control

        action.started += ToggleJetpackAction;
        action.canceled += ToggleJetpackAction;
    }

    private void OnDestroy()
    {
        action.started -= ToggleJetpackAction;
        action.canceled -= ToggleJetpackAction;
    }

    private void OnEnable()
    {
        action.Enable();
    }

    private void OnDisable()
    {
        action.Disable();
    }

    private void FixedUpdate()
    {
        if (isFlying)
        {
            Thrust();
        }
    }

    private void ToggleJetpackAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isFlying = !isFlying;

            if (isFlying)
            {
                rb.useGravity = false; // Turn off gravity when flying
                rb.isKinematic = true; // Use kinematic for direct position control
                OnFlyStart.Invoke();
            }
            else
            {
                rb.useGravity = true; // Turn on gravity when not flying
                rb.isKinematic = false; // Use kinematic for direct position control
                OnFlyStop.Invoke();
            }
        }
    }

    private void Thrust()
    {

        Vector3 flyDirection = (leftHand.position - head.position).normalized;
        float moveDistance = flyingSpeed * Time.fixedDeltaTime;

        RaycastHit hit;
        // Use a raycast to check for collisions in the fly direction
        if (Physics.Raycast(head.position, flyDirection, out hit, moveDistance))
        {
            return; // Stop flying if any collision is detected
        }

        // Calculate the new position
        Vector3 newPosition = rb.position + flyDirection * moveDistance;

        // Use MovePosition to move the Rigidbody to the new position
        rb.MovePosition(newPosition);

        /* todo remove ver 2
        Vector3 flyDirection = (leftHand.position - head.position);
        float moveDistance = flyingSpeed * Time.deltaTime;
        Debug.DrawRay(head.position, flyDirection * moveDistance, Color.red, 0.1f);
        RaycastHit hit;

        if (Physics.Raycast(head.position, flyDirection, out hit, moveDistance))
        {
            return; // Stop flying if any collision is detected
        }
        rb.MovePosition(rb.position + flyDirection * moveDistance);
        */
        /*todo remove this
 * if (Physics.Raycast(head.position, flyDirection, out hit, flyingSpeed * Time.deltaTime, LayerMask.GetMask("Floor")) && hit.transform.CompareTag("Floor")
    || Physics.Raycast(leftHand.position, flyDirection, out hit, flyingSpeed * Time.deltaTime, LayerMask.GetMask("Floor")) && hit.transform.CompareTag("Floor"))
{
    return;
}*/
        //transform.position += flyDirection.normalized * Time.deltaTime * flyingSpeed;
    }

}
