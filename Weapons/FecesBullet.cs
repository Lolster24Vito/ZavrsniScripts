using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FecesBullet : MonoBehaviour
{
    //todo rework this
    public float lifeSeconds = 20f;
    public float postImpactDelay = 2f;         // visible time after hitting something

    [Tooltip("The impulse force applied to an NPC when hit by this bullet.")]
    [SerializeField] private float ragdollImpactForce = 50f; // Force is now tunable
    Rigidbody rb;
    [SerializeField] private bool enableGravityOnLaunch = true; // set true if you want arcs
    private Vector3 direction = Vector3.zero;

    private float bulletSpeed = 10f;
    // private bool isInitialized = false;
    private bool hasCollided = false;
    private bool isReturning = false;          // prevents double-return
    private bool pendingStartLife = false;     // used if Initialize is called while inactive
    //decal settings
    private float lastDecalTime = -1f;
    [SerializeField] private float decalCooldown = 0.5f;  // seconds

    private Coroutine lifeCoroutine = null;
    private Coroutine returnCoroutine = null;

    public System.Action<GameObject> onBulletDie;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //        Destroy(gameObject, lifeSeconds);
    }
    public void Initialize(Vector3 position, Vector3 dir, float bulletSpeed)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        this.bulletSpeed = bulletSpeed;
        // Place object exactly where caller wants (sync both transform & rigidbody)
        transform.position = position;
        // IMPORTANT: sync Rigidbody position to avoid physics teleport/step mismatch
        rb.position = position; // keeps Rigidbody internal state aligned with transform. See notes.


        // safety guard
        if (dir.sqrMagnitude <= Mathf.Epsilon)
        {
            dir = transform.forward; // fallback
        }

        direction = dir.normalized;
        // reset flags/state
        hasCollided = false;

        // ensure not already in returning state
        isReturning = false;

        // reset physics
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = false;

        rb.useGravity = enableGravityOnLaunch;

        rb.velocity = direction * bulletSpeed; // <-- core change


        // cancel old coroutines if any (shouldn't usually exist on well-returned bullets)
        if (lifeCoroutine != null) StopCoroutine(lifeCoroutine);
        if (returnCoroutine != null) StopCoroutine(returnCoroutine);
        //very sus code
        // start the life timer if we're active; otherwise defer to OnEnable
        if (gameObject.activeInHierarchy)
        {
            lifeCoroutine = StartCoroutine(LifeCoroutine());
            pendingStartLife = false;
        }
        else
        {
            // Defer starting the life timer until the object is activated
            pendingStartLife = true;
        }
    }

    private void OnEnable()
    {
        // If Initialize was called while inactive and requested a life timer start, start it now.
        if (pendingStartLife)
        {
            if (lifeCoroutine != null) StopCoroutine(lifeCoroutine);
            lifeCoroutine = StartCoroutine(LifeCoroutine());
            pendingStartLife = false;
        }
    }    // Coroutine to handle the lifeSeconds timer.

    /*
    void FixedUpdate()
    {
        if (!isInitialized) return;
        // Only move the bullet once it has been initialized
            // Move the Rigidbody to a new position along its direction vector
            // This is more direct than using velocity and ignores drag/forces.
            Vector3 next = rb.position + direction * bulletSpeed * Time.fixedDeltaTime;
            rb.MovePosition(next);
    }*/

    private void OnCollisionEnter(Collision collision)
    {

        // Schedule return after a short delay so player sees bounce/impact
        ScheduleReturn(postImpactDelay);
        if (!hasCollided)
        {
            hasCollided = true;
            rb.useGravity = true;
            if (collision.gameObject.tag.Equals("Car"))
            {

                VehicleHitHandler vehicle = collision.gameObject.GetComponent<VehicleHitHandler>();
                if (vehicle != null)
                {
                    vehicle.RegisterHit();
                    SpawnDecalByCollision(collision); // Just a decal for cars
                    return;
                }

            }
            else
            if ((collision.gameObject.layer == LayerMask.NameToLayer("NPC") ||
            collision.gameObject.layer == LayerMask.NameToLayer("NPC_Ragdoll")
            ))
            {
                //Ragdoll logic

                Vector3 impactDir = (collision.transform.position - transform.position).normalized;

                RagdollSwapper.Instance.SwapToRagdoll(
            collision.gameObject,
            ragdollImpactForce,
            collision.GetContact(0).point,
            impactDir
        );
                //old code
                /*
                Debug.Log(" ON COLLISION WITH Feces with ragdoll");
                //forceDirection and flapVelocity get through 
                Ragdoll collisionRagdoll = collision.gameObject.GetComponentInParent<Ragdoll>();
                if (collisionRagdoll != null)
                {
                    Transform parentDecals = collisionRagdoll.TriggerRagdoll(ragdollImpactForce, collision.GetContact(0).point, direction);
                    SpawnDecalByCollision(collision, parentDecals) ;
                }
                SpawnDecalByCollision(collision,collision.gameObject.transform);
                */
            }
            else
            {
                SpawnDecalByCollision(collision);
            }
        }

        //this is sus
        // Dampen motion slightly to show impact (still allow bounce/roll)
        // rb.velocity *= 0.45f;


    }

    private void SpawnDecalByCollision(Collision collision, Transform parent = null)
    {
        if (Time.time - lastDecalTime > decalCooldown)
        {
            // Spawn bullet decal via manager
            ContactPoint contact = collision.GetContact(0);
            Transform finalParent = collision.transform;

            if (parent == null)
            {
                parent = collision.transform;
            }
            DecalManager.Instance?.SpawnDecal(contact.point, contact.normal, parent);
            lastDecalTime = Time.time;

        }
    }

    // Reset on exit so future OnCollisionEnter with other surfaces will run again
    private void OnCollisionExit(Collision collision)
    {
        hasCollided = false;
    }
    private IEnumerator LifeCoroutine()
    {
        yield return new WaitForSeconds(lifeSeconds);
        //onBulletDie?.Invoke(gameObject); // return to pool   
        // If it hasn't collided, schedule immediate return (or short visible time if you prefer)
        ScheduleReturn(0f);
    }

    // optional: called by pool on release to reset state immediately
    public void OnReturnedToPool()
    {
        if (lifeCoroutine != null) { StopCoroutine(lifeCoroutine); lifeCoroutine = null; }
        if (returnCoroutine != null) { StopCoroutine(returnCoroutine); returnCoroutine = null; }

        hasCollided = false;
        isReturning = false;
        pendingStartLife = false;

        direction = Vector3.zero;
        bulletSpeed = 0f;

        // reset physics
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true; // keep it still while pooled
        }
    }

    /// <summary>
    /// Schedule a (single) return to pool after delaySeconds. Idempotent.
    /// </summary>
    /// <param name="delaySeconds"></param>
    private void ScheduleReturn(float delaySeconds)
    {
        // if we already scheduled return, do nothing
        if (isReturning) return;

        isReturning = true;

        // cancel life coroutine if running
        if (lifeCoroutine != null)
        {
            StopCoroutine(lifeCoroutine);
            lifeCoroutine = null;
        }

        // ensure any previous return coroutine cancelled
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
        // If delay is zero, return immediately on this frame (avoids needing a coroutine).
        if (delaySeconds <= 0f)
        {
            DoReturnToPool();
            return;
        }
        // If object is active we can start a coroutine safely.
        // If it's already inactive (rare), just call DoReturnToPool which will be idempotent.
        if (gameObject.activeInHierarchy)
        {
            returnCoroutine = StartCoroutine(ReturnAfterDelay(delaySeconds));
        }
        else
        {
            DoReturnToPool();
        }
        // start delayed return
    }
    private IEnumerator ReturnAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        returnCoroutine = null;
        DoReturnToPool();
    }
    /// <summary>
    /// Perform the actual return callback exactly once.
    /// </summary>
    private void DoReturnToPool()
    {
        // guard double-invoke
        if (!isReturning)
        {
            isReturning = true;
        }

        // make sure coroutines are stopped
        if (lifeCoroutine != null) { StopCoroutine(lifeCoroutine); lifeCoroutine = null; }
        if (returnCoroutine != null) { StopCoroutine(returnCoroutine); returnCoroutine = null; }
        // notify pool (defensive null check)
        try
        {
            onBulletDie?.Invoke(gameObject);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"FecesBullet.DoReturnToPool: callback threw: {e}");
        }

    }

}
