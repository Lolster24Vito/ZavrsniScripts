using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class Pedestrian : MonoBehaviour
{
    public EntityType entityType;
    public float currentRightOffset = 0f;
    [SerializeField] private float localRightDirectionOffsetStrength = 0f;
    [SerializeField] private float localRightOffsetNpcCollision = 8f;


    [SerializeField] private NodePoint currentStartNodePoint;
    [SerializeField] private Vector3 currentTargetDestination = Vector3.zero;
    [SerializeField] private Vector3 currentWorldRecenterOffset = Vector3.zero;

    [SerializeField] private NodePoint currentEndTargetDestination;

    [SerializeField] private List<Vector3> path = new List<Vector3>();

    [SerializeField] private List<Vector3> groupNextPath = new List<Vector3>();
    private List<Pedestrian> groupWithSamePath = new List<Pedestrian>();

    private bool findingPath = false;
    // Offset position by moving slightly in the right local direction when close(on trigger enter) by faster object
    private Vector3 offset;
    private Vector3 targetWithOffset;

    protected float minDistanceForCompletion = 25f;
    private int pathListIndex = 0;
    private bool firstPathFindCalled = false;

    private Quaternion _lookRotation;
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotationSpeed = 20f;
    public bool isEndPointOnSpawnSet = false;
    public Vector3 endPoint;

    private int maxGroupNumber = 4;
    private int randomGroupNumber = 1;
    //bool that represents if object was spawned by another pedestrian
    [HideInInspector] public bool groupSpawned = false;
    private float groupIndividualWaitTime;
    //bool that represents if it should spawn a group
    public bool SpawnGroup = false;
    int pathFoundCount = 0;

    protected float randomSpeedMinimumOffset = -1f;
    protected float randomSpeedMaximumOffset = 3f;


    private CancellationTokenSource pathfindingCancellationTokenSource;
    private List<Vector3> getPositionNodePointTransformNeighbourPoints = new List<Vector3>();
    bool recenteredOnce = false;
    private Vector3 spawnRecenterOffset;
   [SerializeField] private Vector2Int tile = new Vector2Int(-1, -1);
    private Transform cachedTransform;
    private Vector3 targetWithOffsetWithPlayerY;

    public static int npcCount;

    [SerializeField] private float pathfindingCooldown = 1f;
    private float lastPathfindingTime = 0f;

    public void SetTile(Vector2Int setTile)
    {
        tile = setTile;
    }
    public Vector2Int GetTile()
    {
        return tile;
    }
    private void Awake()
    {
        cachedTransform = transform;
    }
    protected virtual void Start()
    {
        npcCount++;
        randomGroupNumber = UnityEngine.Random.Range(1, maxGroupNumber);
        float randomSpeedOffset = UnityEngine.Random.Range(randomSpeedMinimumOffset, randomSpeedMaximumOffset);
        moveSpeed += randomSpeedOffset;
        currentRightOffset = localRightDirectionOffsetStrength;
        currentWorldRecenterOffset = Vector3.zero;
        spawnRecenterOffset = WorldRecenterManager.Instance.GetRecenterOffset();
        WorldRecenterManager.OnWorldRecentered += SetToNewestRecenterOffset;
    }

    internal void SetStartingNode(NodePoint randomPosition)
    {
        currentStartNodePoint = randomPosition;
    }

    private void OnEnable()
    {
        if(!targetWithOffset.Equals(Vector3.zero))
        transform.position = targetWithOffset;
    }

    private void OnDisable()
    {
        npcCount--;
    }

    private void OnDestroy()
    {
        WorldRecenterManager.OnWorldRecentered -= SetToNewestRecenterOffset;

        pathfindingCancellationTokenSource?.Cancel();
        npcCount--;


    }
    private void SetToNewestRecenterOffset(Vector3 offset)
    {
        currentWorldRecenterOffset -= offset;
    }
    private Vector3 tileManagerOffset;

    public Vector3 getCurrentTarget()
    {
        return targetWithOffset;
    }
    protected virtual void FixedUpdate()
    {

        if (!firstPathFindCalled && !findingPath && PedestrianDestinations.Instance.IsPathFindingReady())
        {
            if (PedestrianDestinations.Instance.IsPathFindingReady())
            {
                findingPath = true;
                InitializePathfinding();
            }
            return;
        }
        // Offset position by moving slightly in the rightward direction when colliding with slower entity's,and in general.
        offset = cachedTransform.right * currentRightOffset;
        offset.y = 0f;
        targetWithOffset = currentTargetDestination + offset;

       if( TileManager.TryGetOffset(tile, out tileManagerOffset))
        {
            targetWithOffset -=  (WorldRecenterManager.Instance.GetCustomWorldOffsetWithoutFirst())- tileManagerOffset;
        }
        else
        {
            Debug.Log($"VITO Tile {tile} does not exist in the dictionary.");
        }


        if (pathListIndex == 0) cachedTransform.position = targetWithOffset;

        // Check if the pedestrian has reached the destination
        targetWithOffsetWithPlayerY.Set(targetWithOffset.x, cachedTransform.position.y, targetWithOffset.z);
        if (Vector3.Distance(cachedTransform.position, targetWithOffsetWithPlayerY) < minDistanceForCompletion)
        {
            //still has points in path
            if (pathListIndex < path.Count - 1)
            {
                pathListIndex++;
                currentTargetDestination = path[pathListIndex];
            }
            else if (!findingPath)
            {

                if (groupSpawned && groupNextPath.Count > 1)
                {
                    path.Clear();
                    path.AddRange(groupNextPath);
                    cachedTransform.position = path[0];
                    groupNextPath.Clear();
                    pathListIndex = 0;
                    return;
                }
                if (pathFoundCount > 5)
                {
                    currentStartNodePoint = PedestrianDestinations.Instance.GetRandomNodePoint(entityType,tile);
                    cachedTransform.position = currentStartNodePoint.Position;
                    currentEndTargetDestination = PedestrianDestinations.Instance.GetRandomNodePoint(entityType,tile);
                    Debug.Log($"VITO object {gameObject.name} found path over 5 times");
                    findingPath = true;
                    FindPath();
                    pathFoundCount = 0;
                }
                else
                {
                    findingPath = true;
                    currentStartNodePoint = GetPositionNodePoint(cachedTransform.position);
                    FindPath();
                }

            }
        }

        else
        {
            if (firstPathFindCalled)
            {
                MoveTowardsTarget();
            }
        }

    }



    protected void InitializePathfinding()
    {
        if(currentStartNodePoint==null)
        currentStartNodePoint = GetPositionNodePoint(cachedTransform.position);

        // Only set position when we actually found a valid node
        if (currentStartNodePoint != null)
        {
            cachedTransform.position = currentStartNodePoint.Position;
        }
        Debug.Log("Now really started pedestrian");
        if (isEndPointOnSpawnSet)
        {
            currentEndTargetDestination = GetPositionNodePoint(endPoint);

        }
        else
        {
            currentEndTargetDestination = PedestrianDestinations.Instance.GetRandomNodePoint(entityType);
        }
        if (!groupSpawned)
            FindPath();

    }

    private NodePoint GetPositionNodePoint(Vector3 position)
    {
      
        Vector3 tileManagerOffset;

        if (TileManager.TryGetOffset(tile, out tileManagerOffset))
        {
            position += (WorldRecenterManager.Instance.GetCustomWorldOffsetWithoutFirst() - tileManagerOffset);


        }
        NodePoint returnNodePoint = PedestrianDestinations.Instance.GetPoint(position);
        getPositionNodePointTransformNeighbourPoints.Clear();

        if (returnNodePoint == null)
        {
            getPositionNodePointTransformNeighbourPoints = PedestrianDestinations.Instance.GetNeighboursRadialSearch(position, 100f);

            if (getPositionNodePointTransformNeighbourPoints != null && getPositionNodePointTransformNeighbourPoints.Count > 0)
            {
                foreach (Vector3 neighbourPos in getPositionNodePointTransformNeighbourPoints)
                {
                    NodePoint positionNode = PedestrianDestinations.Instance.GetPoint(neighbourPos);
                    if (positionNode != null)
                    {
                        if (entityType.Equals(EntityType.Pedestrian) && positionNode.Type.Equals(NodeType.Sidewalk))
                        {
                            returnNodePoint = positionNode;
                            break;
                        }
                        if (entityType.Equals(EntityType.Car) && positionNode.Type.Equals(NodeType.Sidewalk))
                        {
                            returnNodePoint = positionNode;
                            break;
                        }
                    }
                }
                if (returnNodePoint == null)
                    returnNodePoint = PedestrianDestinations.Instance.GetPoint(getPositionNodePointTransformNeighbourPoints[0]);

            }
            else
            {
                returnNodePoint = PedestrianDestinations.Instance.GetRandomNodePoint(entityType,tile);

            }
        }
        return returnNodePoint;
    }

    protected async void FindPath()
    {
        pathfindingCancellationTokenSource?.Cancel(); // Cancel any ongoing pathfinding task
        pathfindingCancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = pathfindingCancellationTokenSource.Token;
        if (currentStartNodePoint == null) currentStartNodePoint = GetPositionNodePoint(cachedTransform.position);

        currentEndTargetDestination = PedestrianDestinations.Instance.GetRandomNodePoint(entityType);
        try
        {
            path = await PedestrianDestinations.Instance.FindPathAsync(currentStartNodePoint, currentEndTargetDestination, entityType, token);
            pathListIndex = 0;
            currentTargetDestination = path.Count > 0 ? path[0] : cachedTransform.position;
            firstPathFindCalled = true;
            findingPath = false;
            // Spawn group after path calculation
            bool isFirstSpawn = false;
            if (SpawnGroup)
            {
                SpawnGroup = false;
                SpawnGroupWithOffset(randomGroupNumber);
                isFirstSpawn = true;
            }
            pathFoundCount++;
            //on instantiate the path is already copied
            if (!isFirstSpawn)
                NotifyGroupOfNextPath(path);
        }
        catch (Exception ex)
        {
            //Debug.LogError($"Pedestrian pathfinding encountered an error: {ex}");
        }
        finally
        {
            pathfindingCancellationTokenSource?.Dispose();
            pathfindingCancellationTokenSource = null;
            findingPath = false;
        }




    }



    private void NotifyGroupOfNextPath(List<Vector3> resultPath)
    {
        foreach (Pedestrian pedestrian in groupWithSamePath)
        {
            pedestrian.BeNotifiedOfNextPath(resultPath);
        }
    }
    public void BeNotifiedOfNextPath(List<Vector3> resultPath)
    {
        if (path.Count == 0 || pathListIndex == path.Count)
        {
            this.path.Clear();
            pathListIndex = 0;
            this.path.AddRange(resultPath);

        }
        else
        {
            groupNextPath.Clear();
            groupNextPath.AddRange(resultPath);
        }

    }

    private void SpawnGroupWithOffset(int groupSize)
    {
        Vector3[] directions = {
            cachedTransform.forward, -cachedTransform.forward,
            cachedTransform.right, -cachedTransform.right
        };
        for (int i = 0; i < groupSize; i++)
        {
            Vector3 offsetDirection = directions[i % directions.Length];
            Vector3 groupOffset = offsetDirection * localRightDirectionOffsetStrength;
            groupOffset.y = 0;

            GameObject pedestrianClone = Instantiate(gameObject, cachedTransform.position + groupOffset, Quaternion.identity, cachedTransform.parent);
            pedestrianClone.name += i.ToString();
            Pedestrian pedestrianScript = pedestrianClone.GetComponent<Pedestrian>();
            pedestrianScript.pathListIndex = 0;
            pedestrianScript.groupSpawned = true;
            pedestrianScript.tile = tile;
            float waitTime = (i + 1) * 1.5f;
            pedestrianScript.groupIndividualWaitTime = Time.time + waitTime;
            pedestrianScript.findingPath = false;
            pedestrianScript.endPoint = currentEndTargetDestination.Position;
            pedestrianScript.firstPathFindCalled = true;

            groupWithSamePath.Add(pedestrianScript);
        }
        foreach (Pedestrian pedestrian in groupWithSamePath)
        {
            pedestrian.AddGroup(groupWithSamePath, this);
        }
        groupSpawned = true;
    }
    public void AddGroup(List<Pedestrian> group, Pedestrian caller)
    {
        groupWithSamePath.Add(caller);
        foreach (Pedestrian pedestrian in group)
        {
            if (pedestrian != this)
                groupWithSamePath.Add(pedestrian);
        }
    }

    private void MoveTowardsTarget()
    {
        //spawned group cars need to be spaced 
        if (entityType.Equals(EntityType.Car) && groupSpawned && groupIndividualWaitTime > Time.time) return;

        //moveToPoint;
        Vector3 directionToPoint = (targetWithOffset - cachedTransform.position).normalized;

        cachedTransform.position += directionToPoint * moveSpeed * Time.deltaTime;

        //create the rotation we need to be in to look at the target
        _lookRotation = Quaternion.LookRotation(directionToPoint);
        Vector3 adjustedEulerAngles = _lookRotation.eulerAngles;
        adjustedEulerAngles.x = 0; // Lock the X-axis
        _lookRotation = Quaternion.Euler(adjustedEulerAngles);
        cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, _lookRotation, Time.deltaTime * rotationSpeed);

    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("NPC"))
        {

            Pedestrian çollidedNPC = collision.gameObject.GetComponent<Pedestrian>();
            if (çollidedNPC != null)
            {

                if (moveSpeed > çollidedNPC.GetMoveSpeed())
                {
                    currentRightOffset = localRightDirectionOffsetStrength + localRightOffsetNpcCollision;
                }
                else
                {
                    currentRightOffset = localRightDirectionOffsetStrength;
                }
            }

        }

    }
    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("NPC"))
        {
            Pedestrian çollidedNPC = collision.gameObject.GetComponent<Pedestrian>();
            if (moveSpeed > çollidedNPC.GetMoveSpeed())
            {
                currentRightOffset = localRightDirectionOffsetStrength;
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("NPC"))
        {
            Pedestrian çollidedNPC = collision.gameObject.GetComponent<Pedestrian>();

            if (moveSpeed > çollidedNPC.GetMoveSpeed())
            {
                currentRightOffset = localRightDirectionOffsetStrength + localRightOffsetNpcCollision;
            }
            else
            {
                currentRightOffset = localRightDirectionOffsetStrength;
            }
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("NPC"))
        {
            Pedestrian çollidedNPC = collision.gameObject.GetComponent<Pedestrian>();

            if (moveSpeed > çollidedNPC.GetMoveSpeed())
            {
                currentRightOffset = localRightDirectionOffsetStrength;
            }
        }
    }

    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        if (firstPathFindCalled && currentTargetDestination != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentTargetDestination, 1);

        }
    }
    public float GetMoveSpeed()
    {
        return moveSpeed;
    }
    

}
