using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianOld : MonoBehaviour
{
    [SerializeField]private Vector3 currentTargetDestination=Vector3.zero;
    private NavMeshAgent agent;
    [SerializeField] Vector3 firstDestination;
    private bool firstDestinationVisited = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        SetDestination(firstDestination);
    }
    private void Update()
    {
        
        // Check if the pedestrian has reached the destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
       //     currentTargetDestination = PedestrianDestinations.Instance.GetRandomPedestrianPoint(EntityType.Pedestrian);
            SetDestination(currentTargetDestination);
            // Optionally, do something when the pedestrian reaches the destination
        }
        
    }

    private void SetDestination(Vector3 targetDestination)
    {

        if (targetDestination != null)
        {
            
            agent.SetDestination(targetDestination);
            
        }
        else
        {
            Debug.LogError("Target destination is not assigned!");
        }
    }
}
