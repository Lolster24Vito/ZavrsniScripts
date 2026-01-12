using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollRecovery : MonoBehaviour
{
    public Vector2Int pedestrianTile;
    public Vector3 originalPosition;
    public bool forceRecovery = false;

    public EntityType entityType;
    private Ragdoll ragdoll;
    private Pedestrian pedestrian; // On ragdoll prefab


    void Awake()
    {
        ragdoll = GetComponent<Ragdoll>();
        //test if pedestrian is needed
        pedestrian = GetComponent<Pedestrian>(); // Get from ragdoll prefab
        if (ragdoll == null)  // ? NULL CHECK ADDED
        {
            Debug.LogError("RagdollRecovery requires Ragdoll component!");
            enabled = false;
        }
    }

    void Update()
    {
        if (ragdoll == null) return;

        // Check if ragdoll is done (back to walking state)
        if (forceRecovery || ragdoll.GetRagdollState() == Ragdoll.RagdollState.Walking)
        {
            SwapBackToPedestrian();
        }
    }

    public void ForceRecovery()
    {
        forceRecovery = true;
    }

    void SwapBackToPedestrian()
    {
        // 1. Get new pedestrian from pool
        GameObject newPedestrian = NpcPoolManager.Instance.GetPedestrian();
        if (newPedestrian == null) return;

        // 2. Position at ragdoll's position
        Transform hips = ragdoll.hipsBone;
        newPedestrian.transform.position = hips != null ? hips.position : transform.position;

        // 3. Setup pedestrian
        Pedestrian pedScript = newPedestrian.GetComponent<Pedestrian>();
        if (pedScript != null)
        {
            pedScript.entityType = entityType;
            pedScript.SetTile(pedestrianTile);
            pedScript.ActivateFromPool(originalPosition, null, pedestrianTile);

            // Restore pathfinding
            NodePoint nearestNode = FindNearestNode(newPedestrian.transform.position, pedScript.entityType);
            if (nearestNode != null)
            {
                pedScript.currentStartNodePoint = nearestNode;
                // pedScript.FindPath();
                if (pedScript.path.Count == 0)
                {
                    //sus
                    pedScript.FindPath();
                }
            }
        }

        // 4. Return ragdoll to pool
        NpcPoolManager.Instance.ReleaseRagdollPedestrian(gameObject);
        RagdollSwapper.Instance.NotifyRagdollRecovered(gameObject);

        // 5. Remove this component
        Destroy(this);
    }

    private NodePoint FindNearestNode(Vector3 position, EntityType entityType)
    {
        // Simple: Use your existing PedestrianDestinations
        NodePoint node = PedestrianDestinations.Instance.GetPoint(position);
        if (node == null)
        {
            // Fallback: Get random node of correct type
            node = PedestrianDestinations.Instance.GetRandomNodePoint(entityType, pedestrianTile);
        }
        return node;
    }
}