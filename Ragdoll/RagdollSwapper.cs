using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RagdollSwapper : MonoBehaviour
{
    public static RagdollSwapper Instance;
    [Tooltip("Maximum number of ragdolls active in the scene at once.")]
    [SerializeField] private int maxActiveRagdolls = 5;
    [Tooltip("Distance from the player camera beyond which a ragdoll is eligible for replacement.")]
    [SerializeField] private float priorityDistance = 15f;

    private List<GameObject> activeRagdolls = new List<GameObject>();
    private Transform playerCamera;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // MULTIPLE FALLBACK METHODS ADDED:
        OVRCameraRig ovrRig = FindObjectOfType<OVRCameraRig>();
        if (ovrRig != null)
        {
            playerCamera = ovrRig.centerEyeAnchor;  // PRIMARY VR METHOD
        }
        else
        {
            playerCamera = Camera.main?.transform;  //  FALLBACK 1
        }

        if (playerCamera == null)
            Debug.LogWarning("VR Camera not found! Ragdoll priority system disabled.");  //  WARNING
    }

    public bool SwapToRagdoll(GameObject pedestrian, float force, Vector3 contactPoint, Vector3 direction, Vector3? decalNormal = null)
    {

        if (pedestrian == null) return false;

        Pedestrian pedScript = pedestrian.GetComponent<Pedestrian>();
        if (pedScript == null) return false;

        if (pedScript.entityType == EntityType.Car)
        {
            Debug.Log("Hit a car - skipping ragdoll swap.");
            return false;
        }

        // Check if we're at max ragdolls
        if (activeRagdolls.Count >= maxActiveRagdolls)
        {
            // Try to replace a distant ragdoll. If we can't, exit (no ragdoll/reaction).
            if (!TryReplaceDistantRagdoll(pedestrian.transform.position))
            {
                return false;
            }
        }


        // --- 1. GET STATE & POSE ---
        Vector2Int tile = pedScript.GetTile();
        Vector3 position = pedestrian.transform.position;
        Quaternion rotation = pedestrian.transform.rotation;
        Animator pedestrianAnimator = pedestrian.GetComponent<Animator>();

        // 2. Get ragdoll from pool
        GameObject ragdoll = NpcPoolManager.Instance.GetRagdollPedestrian();
        if (ragdoll == null) return false;

        // Position the ragdoll
        ragdoll.transform.position = position;
        ragdoll.transform.rotation = rotation;
        MatchRagdollToPose(ragdoll, pedestrianAnimator); // ** Uses the superior full-bone matching **

        // 3. Activate ragdoll physics
        Ragdoll ragdollComp = ragdoll.GetComponent<Ragdoll>();
        if (ragdollComp != null)
        {
            ragdollComp.TriggerRagdoll(force, contactPoint, direction);
        }
        // --- NEW: HANDLE DECAL ATTACHMENT ---
        // We do this AFTER matching pose so the bones are in the correct hit position
        if (decalNormal.HasValue)
        {
            // Find which bone on the NEW ragdoll is closest to where the bullet hit
            Transform targetBone = GetClosestRagdollBone(ragdoll, contactPoint);

            // Spawn the decal attached to that moving dynamic bone
            DecalManager.Instance?.SpawnDecal(contactPoint, decalNormal.Value, targetBone);
        }
        // -------------------------------------
        // 4. Store recovery info on ragdoll
        RagdollRecovery recovery = ragdoll.AddComponent<RagdollRecovery>();
        recovery.pedestrianTile = tile;
        recovery.originalPosition = position;
        recovery.entityType = pedScript.entityType; //todo


        // 5. CLEANUP Return pedestrian to pool
        NpcPoolManager.Instance.ReleasePedestrian(pedestrian);
        // 6. TRACKING Track active ragdoll
        activeRagdolls.Add(ragdoll);

        return true;
    }
    // --- NEW HELPER METHOD ---
    private Transform GetClosestRagdollBone(GameObject ragdoll, Vector3 hitPosition)
    {
        // We look for Rigidbodies because they represent the physical bones (Hips, Spine, Head, etc.)
        // This ignores the empty container or LOD objects
        Rigidbody[] bones = ragdoll.GetComponentsInChildren<Rigidbody>();

        Transform bestBone = ragdoll.transform;
        float closestDistSqr = Mathf.Infinity;

        foreach (var rb in bones)
        {
            float distSqr = (rb.position - hitPosition).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                bestBone = rb.transform;
            }
        }
        return bestBone;
    }

    private bool TryReplaceDistantRagdoll(Vector3 newNpcPosition)
    {
        if (playerCamera == null) return false;
        activeRagdolls.RemoveAll(r => r == null);

        GameObject farthestRagdoll = null;
        float maxDistance = 0;

        foreach (var ragdoll in activeRagdolls)
        {
            float distance = Vector3.Distance(ragdoll.transform.position, playerCamera.position);
            // Check if ragdoll is farther than the current maximum AND outside the priority zone
            if (distance > maxDistance && distance > priorityDistance)
            {
                maxDistance = distance;
                farthestRagdoll = ragdoll;
            }
        }

        if (farthestRagdoll != null)
        {
            // Force recovery
            RagdollRecovery recovery = farthestRagdoll.GetComponent<RagdollRecovery>();
            if (recovery != null)
            {
                recovery.ForceRecovery();
                return true;
            }
        }

        return false;
    }

    public void NotifyRagdollRecovered(GameObject ragdoll)
    {
        activeRagdolls.Remove(ragdoll);
    }

    private void MatchRagdollToPose(GameObject ragdoll, Animator animator)
    {
        if (animator == null) return;

        Transform[] ragdollBones = ragdoll.GetComponentsInChildren<Transform>();

        // Match all human bones for a smooth transition
        foreach (HumanBodyBones boneType in System.Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (boneType == HumanBodyBones.LastBone) continue;

            Transform animatorBone = animator.GetBoneTransform(boneType);
            if (animatorBone == null) continue;

            // Find the corresponding ragdoll bone by name
            Transform ragdollBone = System.Array.Find(ragdollBones,
                t => t.name == animatorBone.name);

            if (ragdollBone != null)
            {
                ragdollBone.position = animatorBone.position;
                ragdollBone.rotation = animatorBone.rotation;
            }
        }
    }
}