using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Ragdoll : MonoBehaviour
{
    private enum RagdollState
    {
        Walking,
        Ragdoll,
        StandingUp
    }

    private Rigidbody[] ragdollRigidbodies;
    [SerializeField] private GameObject rigGameObject;
    private Collider[] rigColliders;
    private Animator animator;
    private CharacterController characterController;
    private Transform hipsBone;
    private Pedestrian pedestrian;
    private CapsuleCollider capsuleCollider;

    private readonly float ragdollDuration = 10f;
    private const string standUpStateName = "Stand Up";
    private float timeToWakeUp = 0f;
    private RagdollState ragdollState;

    void Awake()
    {
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
        pedestrian = GetComponent<Pedestrian>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        rigColliders = rigGameObject.GetComponentsInChildren<Collider>();
        DisableRagdoll();
    }

    public Transform TriggerRagdoll(float force, Vector3 contactPoint, Vector3 aimingDirection)
    {
        EnableRagdoll();
        Rigidbody hitRigidBody = ragdollRigidbodies.OrderBy(rb => Vector3.Distance(rb.position, contactPoint)).First();
        hitRigidBody.AddForceAtPosition(force * aimingDirection, contactPoint, ForceMode.Impulse);
        return hitRigidBody.transform;
    }

    void Update()
    {

        switch (ragdollState)
        {

            case RagdollState.Ragdoll:
                RagdollDisableBehaviour();
                break;
            case RagdollState.StandingUp:
                StandingUpBehaviour();
                break;
        }

    }

    private void RagdollDisableBehaviour()
    {
        timeToWakeUp -= Time.deltaTime;
        if (timeToWakeUp <= 0)
        {

            // 1. Gather data
            // (Even though the script is disabled, we can still read its public variables)
            EntityType typeToRespawn = pedestrian.entityType;
            Vector2Int currentTile = pedestrian.GetTile();

            // 2. Find a valid position for the new entity
            // We don't want to spawn exactly where the body is (it might be off the navmesh).
            // We ask the navigation system for a fresh random point on the same tile.
            NodePoint spawnNode = PedestrianDestinations.Instance.GetNearestValidNode(
                        transform.position,
                        typeToRespawn,
                        currentTile
                    );
            // 3. Request Spawn
            if (DotsSpawnerBridge.Instance != null && spawnNode != null)
            {
                DotsSpawnerBridge.Instance.RequestSpawn(typeToRespawn, currentTile, spawnNode.Position);
            }

            // 4. Release Ragdoll

            // --- FIX END ---

            NpcPoolManager.Instance.ReleasePedestrianRagdoll(this.gameObject);
        }

    }

    private void DisableRagdoll()
    {
        foreach (Rigidbody rigidbody in ragdollRigidbodies)
        {
            rigidbody.isKinematic = true;
        }
        foreach (Collider col in rigColliders)
        {
            col.enabled = false;
        }
        capsuleCollider.enabled = true;
        animator.enabled = true;
        if(characterController!=null)
        {
        characterController.enabled = true;
        }
        ragdollState = RagdollState.Walking;
    }
    private void EnableRagdoll()
    {
        foreach (Rigidbody rigidbody in ragdollRigidbodies)
        {
            rigidbody.isKinematic = false;
        }
        foreach (Collider col in rigColliders)
        {
            col.enabled = true;
        }
        capsuleCollider.enabled = false;

        animator.enabled = false;
                if(characterController!=null)
                {
                
        characterController.enabled = false;
                        }

        ragdollState = RagdollState.Ragdoll;
        pedestrian.enabled = false;
        timeToWakeUp = ragdollDuration;
    }
    private void AlignPositionToHips()
    {
        Vector3 originalHipsPosition = hipsBone.position;
        transform.position = hipsBone.position;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo))
        {
            transform.position = new Vector3(transform.position.x, hitInfo.point.y, transform.position.z);
        }
        hipsBone.position = originalHipsPosition;
    }
    private void StandingUpBehaviour() 
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(standUpStateName) == false)
        {
            // todo VITO see if this messes up the game
            //old working code
            //ragdollState = RagdollState.Walking;
            //pedestrian.enabled = true;
            NpcPoolManager.Instance.ReleasePedestrianRagdoll(this.gameObject);
        }
    }
    //this is used to reset the ragdoll state when pooling.
    public void ResetRagdoll()
    {
        DisableRagdoll();
    }
}
