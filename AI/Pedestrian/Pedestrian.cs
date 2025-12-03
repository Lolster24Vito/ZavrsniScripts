using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms; // Required for LocalTransform
using Unity.Mathematics;

public class Pedestrian : MonoBehaviour
{
    public EntityType entityType;
    public float currentRightOffset = 0f;
    [SerializeField] private float localRightDirectionOffsetStrength = 0f;
    [SerializeField] private float localRightOffsetNpcCollision = 8f;

    [SerializeField] private bool useCharacterController = false;

    [SerializeField] private Vector3 currentWorldRecenterOffset = Vector3.zero;

    [SerializeField] private NodePoint currentStartNodePoint;
    [SerializeField] private Vector3 currentTargetDestination = Vector3.zero;

    [SerializeField] private NodePoint currentEndTargetDestination;

    [SerializeField] private List<Vector3> path = new List<Vector3>();

    [SerializeField] private List<Vector3> groupNextPath = new List<Vector3>();
    private List<Pedestrian> groupWithSamePath = new List<Pedestrian>();

    public bool findingPath = false;
    // Offset position by moving slightly in the right local direction when close(on trigger enter) by faster object
    private Vector3 offset;
    private Vector3 targetWithOffset;

    [SerializeField] protected float minDistanceForCompletion = 0.5f;
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
    [SerializeField] private Vector2Int tile = new Vector2Int(-1, -1);
    private Transform cachedTransform;
    private Vector3 targetWithOffsetWithPlayerY;

    public static int npcCount;

    [SerializeField] private float pathfindingCooldown = 1f;
    private float lastPathfindingTime = 0f;

    private CharacterController characterController;
    private Vector3 tileManagerOffset; 

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
        if (useCharacterController)
        {
            characterController = GetComponent<CharacterController>();
        }
        randomGroupNumber = UnityEngine.Random.Range(1, maxGroupNumber);
        float randomSpeedOffset = UnityEngine.Random.Range(randomSpeedMinimumOffset, randomSpeedMaximumOffset);
        moveSpeed += randomSpeedOffset;
        currentRightOffset = localRightDirectionOffsetStrength;
        currentWorldRecenterOffset = Vector3.zero;
        //this was removed uncomented unlined in this version
        //WorldRecenterManager.OnWorldRecentered += SetToNewestRecenterOffset; 
    }

    internal void SetStartingNode(NodePoint randomPosition)
    {
        currentStartNodePoint = randomPosition;
    }

    private void OnEnable()
    {
        npcCount++;
        WorldRecenterManager.OnWorldRecentered += SetToNewestRecenterOffset;


        if (!targetWithOffset.Equals(Vector3.zero))
            SetPositionAndTeleport(targetWithOffset); // MODIFIED
    }

    private void OnDisable()
    {
        npcCount--;
        // Cancel any ongoing pathfinding when disabled
        WorldRecenterManager.OnWorldRecentered -= SetToNewestRecenterOffset;
        pathfindingCancellationTokenSource?.Cancel();
        pathfindingCancellationTokenSource?.Dispose();
        pathfindingCancellationTokenSource = null;
    }

    private void OnDestroy()
    {
        //new code uncommented
        WorldRecenterManager.OnWorldRecentered -= SetToNewestRecenterOffset;

        //     pathfindingCancellationTokenSource?.Cancel();
        //   npcCount--;


    }
    // --- ADDED: UPDATE LOOP TO SYNC VISUALS ---

    // --- ADDED: SYNC VISUALS FROM DOTS TO GAME OBJECT ---
 
    private void SetToNewestRecenterOffset(Vector3 offset)
    {
        currentWorldRecenterOffset -= offset;
    }

    //private Vector3 tileManagerOffset;

    public Vector3 getCurrentTarget()
    {
        return targetWithOffset;
    }
    private float pathFailureCooldownTimer = 0f;
    private const float PathFailureDelay = 2.0f; // 2-second delay before retrying
    protected virtual void FixedUpdate()
    {
        // Initialization
        if (!firstPathFindCalled && !findingPath && PedestrianDestinations.Instance.IsPathFindingReady())
        {
            InitializePathfinding();
            return;
        }
        if (pathFailureCooldownTimer > 0)
        {
            pathFailureCooldownTimer -= Time.fixedDeltaTime;
        }
        else if (!findingPath && (path == null || path.Count == 0)) // Only start if not finding and no path
        {
            InitializePathfinding(); // Calls FindPath() (line 275)
        }
        // If no path or still finding, don't move
        if (path.Count == 0 || findingPath)
            return;

        // Calculate target with offset
        CalculateTargetWithOffset();

        // Check if reached current target point
        if (HasReachedCurrentTarget())
        {
            AdvanceToNextPointOrGetNewPath();
            return;
        }

        // Move toward target
        MoveTowardsTarget();
    }

    private void CalculateTargetWithOffset()
    {
        offset = cachedTransform.right * currentRightOffset;
        offset.y = 0f;
        targetWithOffset = currentTargetDestination + offset;

        if (TileManager.TryGetOffset(tile, out tileManagerOffset))
        {
            targetWithOffset -= (WorldRecenterManager.Instance.GetCustomWorldOffsetWithoutFirst()) - tileManagerOffset;
        }

        // Teleport to first point when starting
     /*   if (pathListIndex == 0)
        {
            SetPositionAndTeleport(targetWithOffset);
        }*/
    }
    /*
    private bool HasReachedCurrentTarget()
    {
        if (path.Count == 0) return false;

        Vector3 targetFlat = new Vector3(targetWithOffset.x, cachedTransform.position.y, targetWithOffset.z);
        return Vector3.Distance(cachedTransform.position, targetFlat) < minDistanceForCompletion;
    }
    */
    // Pedestrian.cs

    // ... (Around line 160)

    private bool HasReachedCurrentTarget()
    {

        if (path.Count == 0 || pathListIndex >= path.Count)
        {
            return false;
        }
        CalculateTargetWithOffset();
        // The pedestrian is moving towards 'targetWithOffset', which includes all offsets (tile, recenter, right-offset).
        // Compare positions ignoring the Y component to check horizontal distance.
        Vector3 currentPosFlat = cachedTransform.position;
        Vector3 targetFlat = targetWithOffset;

        currentPosFlat.y = 0;
        targetFlat.y = 0;

        float distance = Vector3.Distance(currentPosFlat, targetFlat);
        bool reached = distance < minDistanceForCompletion;

        // Advance to the next point if we are within the required completion distance.
        return reached;
    }
    private void AdvanceToNextPointOrGetNewPath()
    {
        // Move to next point in path if available
        if (pathListIndex < path.Count - 1)
        {
            pathListIndex++;
            currentTargetDestination = path[pathListIndex];
        }
        else // Reached end of path
        {
            HandlePathEndAndRefill();
        }
    }
    //normal MOveTowards code
    /* private void MoveTowardsTarget()
     {
         if (characterController == null || !characterController.enabled) return;

         if (entityType.Equals(EntityType.Car) && groupSpawned && groupIndividualWaitTime > Time.time)
             return;

         Vector3 direction = (targetWithOffset - cachedTransform.position).normalized;
         cachedTransform.position += direction * moveSpeed * Time.deltaTime;

         // Rotate toward target
         if (direction.sqrMagnitude > 0.001f)
         {
             Quaternion targetRot = Quaternion.LookRotation(direction);
             targetRot.eulerAngles = new Vector3(0, targetRot.eulerAngles.y, 0);
             cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, targetRot, rotationSpeed * Time.deltaTime);
         }
     }*/
    //gemini slop:
    private void MoveTowardsTarget()
    {
        if (entityType.Equals(EntityType.Car) && groupSpawned && groupIndividualWaitTime > Time.time)
            return;

        // Calculate direction to target WITH offset (for actual movement)
        Vector3 direction = (targetWithOffset - cachedTransform.position).normalized;

        // If direction is too small, don't move
        if (direction.sqrMagnitude < 0.01f)
            return;

        // Calculate movement
        Vector3 movement = direction * moveSpeed * Time.deltaTime;

        // Use CharacterController if enabled
        if (useCharacterController && characterController != null && characterController.enabled)
        {
            // SimpleMove automatically applies gravity and is framerate-independent
            characterController.SimpleMove(direction * moveSpeed);
        }
        else
        {
            // Direct transform movement (for cars or when CharacterController is disabled)
            cachedTransform.position += movement;
        }

        // Only rotate if we're actually moving a significant amount
        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            targetRotation.eulerAngles = new Vector3(0, targetRotation.eulerAngles.y, 0);
            cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    private void HandlePathEndAndRefill()
    {
        path.Clear();
        pathListIndex = 0;

        Debug.Log($"VITO object {gameObject.name} reached end of path, getting new path");
        if (groupSpawned && groupNextPath.Count > 1)
        {
            path.AddRange(groupNextPath);
            SetPositionAndTeleport(path[0]); 
            groupNextPath.Clear();
            Debug.Log($"VITO object {gameObject.name} found path because of groupNextPath");

            return;
        }
        if (pathFoundCount > 5)//&& !findingPath
        {
            Debug.Log($"VITO object {gameObject.name} found path over 5 times");

            currentStartNodePoint = PedestrianDestinations.Instance.GetRandomNodePoint(entityType, tile);
            SetPositionAndTeleport(currentStartNodePoint.Position);
            pathFoundCount = 0;
//            currentEndTargetDestination = PedestrianDestinations.Instance.GetRandomNodePoint(entityType, tile); //todo change
  //          findingPath = true;
    //        FindPath();
        }
        if(!findingPath)
        {
            Debug.Log($"VITO object {gameObject.name} not finding path is false, calling findPath");
            pathFoundCount++;
         //   findingPath = true;
            currentStartNodePoint = GetPositionNodePoint(cachedTransform.position);
            FindPath();
        }
    }
    protected void InitializePathfinding()
    {
        if (currentStartNodePoint == null)
            currentStartNodePoint = GetPositionNodePoint(cachedTransform.position);

        // Only set position when we actually found a valid node
        if (currentStartNodePoint != null)
        {
            SetPositionAndTeleport(currentStartNodePoint.Position);
            //   cachedTransform.position = currentStartNodePoint.Position;
            // SetPositionAndTeleport(currentStartNodePoint.Position); // MODIFIED

        }
        Debug.Log("Now really started pedestrian: " + gameObject.name);

        if (isEndPointOnSpawnSet)
        {
            Debug.Log("Vito debug first if TRUE " + gameObject.name);
            currentEndTargetDestination = GetPositionNodePoint(endPoint);

        }
        else
        {
            Debug.Log("Vito debug first else TRUE " + gameObject.name);

            currentEndTargetDestination = PedestrianDestinations.Instance.GetRandomNodePoint(entityType);
        }
        if (!groupSpawned)
        {
            Debug.Log("Vito debug 2 if TRUE " + gameObject.name);

            FindPath();

        }
        else
        {
            Debug.Log("Vito debug 2 else TRUE " + gameObject.name);

            // If groupSpawned is true, the leader has already populated the 'path' list.
            // We just need to sync it to DOTS and ensure the state is correct.
            currentTargetDestination = path.Count > 0 ? path[pathListIndex] : cachedTransform.position;
            firstPathFindCalled = true;
            findingPath = false;
        }
        Debug.Log("Vito debug 3 nista TRUE " + gameObject.name);

        //14_11 VITO todo old code  FindPath();


    }
    private NodePoint GetPositionNodePoint(Vector3 position)
    {
        Debug.Log($"GetPositionNodePoint VITO object {gameObject.name} is getting position for {position.ToString()}");
        Vector3 tileManagerOffset;

        if (TileManager.TryGetOffset(tile, out tileManagerOffset))
        {
            position += (WorldRecenterManager.Instance.GetCustomWorldOffsetWithoutFirst() - tileManagerOffset);


        }
        NodePoint returnNodePoint = PedestrianDestinations.Instance.GetPoint(position);
        getPositionNodePointTransformNeighbourPoints.Clear();

        if (returnNodePoint == null)
        {
            getPositionNodePointTransformNeighbourPoints = PedestrianDestinations.Instance.GetNeighboursRadialSearch(position, 10f);

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
                returnNodePoint = PedestrianDestinations.Instance.GetRandomNodePoint(entityType, tile);

            }
        }
        Debug.Log($"GetPositionNodePoint VITO object {gameObject.name} return nodePoint: {returnNodePoint.ToString()}");

        return returnNodePoint;
    }

    protected async void FindPath()
    {
        if (findingPath)
        {
            Debug.Log($"{gameObject.name}: Already finding path, skipping");
            return; // Prevention
        }
        findingPath = true;
        Debug.Log("Gameobject is finding path:" + gameObject.name);
        // Check if pathfinding is ready
        try
        {


            if (!PedestrianDestinations.Instance.IsPathFindingReady())
            {
                Debug.LogWarning("Pathfinding system is not ready yet");
                Debug.Log("Pathfinding system is not ready yet");
                findingPath = false;
                return;
            }

            pathfindingCancellationTokenSource?.Cancel(); // Cancel any ongoing pathfinding task
            pathfindingCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = pathfindingCancellationTokenSource.Token;


            if (currentStartNodePoint == null) currentStartNodePoint = GetPositionNodePoint(cachedTransform.position);
            if (currentEndTargetDestination == null)
            {
                currentEndTargetDestination = PedestrianDestinations.Instance.GetRandomNodePoint(entityType, tile);
                Debug.Log($"{gameObject.name}: Got new end node: {currentEndTargetDestination?.Position}");
            }


            List<Vector3> newPath = await PedestrianDestinations.Instance.FindPathAsync(currentStartNodePoint, currentEndTargetDestination, entityType, token);
            Debug.Log("Vito return path length is :" + path.Count);
            //new code:
            // CHECK RESULT
            if (newPath != null && newPath.Count > 1)
            {
//simple new code varient 3_12_2025

                 float distanceToFirst = Vector3.Distance(cachedTransform.position, newPath[0]);
        float distanceToLast = Vector3.Distance(cachedTransform.position, newPath[newPath.Count - 1]);

        // If the last point is closer, then reverse the path
        if (distanceToLast < distanceToFirst)
        {
            Debug.Log($"{gameObject.name}: Path appears to be reversed. Reversing it.");
            newPath.Reverse();
        }


                //old code backup
                Debug.Log($"Path found successfully for {gameObject.name}. Length: {newPath.Count}");
                path.Clear();
                path.AddRange(newPath);
                pathListIndex = 0;
                if (path.Count > 0)
                {
                    currentTargetDestination = path[0];
                    CalculateTargetWithOffset(); // Update targetWithOffset
                    Debug.Log($"{gameObject.name}: First target set to {currentTargetDestination}");
                }
                firstPathFindCalled = true;
                pathFoundCount++;

                // Success
                // ... (Sync group logic) ...
                // Spawn group after path calculation
              //  bool isFirstSpawn = false;
                if (SpawnGroup&& !groupSpawned)
                {
                    SpawnGroup = false;
                    SpawnGroupWithOffset(randomGroupNumber);
                //    isFirstSpawn = true;
                }
                if (!groupSpawned)
                {
                    NotifyGroupOfNextPath(path);
                }

//                if (!isFirstSpawn)
  //                  NotifyGroupOfNextPath(path);
            }
            else
            {
                // Fail - Wait before retrying!
                Debug.Log($"{gameObject.name}: Path calculation failed or returned empty. Retrying in 0s...");
                currentStartNodePoint = GetPositionNodePoint(cachedTransform.position);

               // await Task.Delay(1000); // Non-blocking wait
                                        // The FixedUpdate loop will pick it up again after this delay
            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"Pathfinding Crash: {ex.Message}");
        }
        finally
        {
            findingPath = false;
            pathfindingCancellationTokenSource?.Dispose();
            pathfindingCancellationTokenSource = null;
            Debug.Log($"{gameObject.name}: Pathfinding completed");

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
        // Add safety check - don't spawn group if leader has no path
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"Leader {gameObject.name} has no path, cannot spawn group");
            return;
        }
        Vector3[] directions = {
            cachedTransform.forward, -cachedTransform.forward,
            cachedTransform.right, -cachedTransform.right
        };
        for (int i = 0; i < groupSize; i++)
        {
            Vector3 offsetDirection = directions[i % directions.Length];
            Vector3 groupOffset = offsetDirection * localRightDirectionOffsetStrength;
            groupOffset.y = 0;
            // Use pool instead of Instantiate

            GameObject pedestrianClone = entityType == EntityType.Pedestrian
                ? NpcPoolManager.Instance.GetPedestrian()
                : NpcPoolManager.Instance.GetCar();
            Debug.Log($"Got from pool: {gameObject.name}_group_num_{i}_tiles_{tile.x}_{tile.y}");
            //manually calling it here because pedestrian script might not be ready for  SetPositionAndTeleport 
            /*   CharacterController cloneCC = pedestrianClone.GetComponent<CharacterController>();
               if (cloneCC != null) cloneCC.enabled = false;
            */
            pedestrianClone.transform.position = cachedTransform.position + groupOffset; // This is the line to modify.

            /* if (cloneCC != null)
             {
                 Physics.SyncTransforms();
                 cloneCC.enabled = true;
                 Physics.SyncTransforms();
             }*/
            //pedestrianClone.transform.position = cachedTransform.position;

            //  pedestrianClone.transform.position = cachedTransform.position + groupOffset;
            pedestrianClone.transform.rotation = Quaternion.identity;
            pedestrianClone.transform.parent = cachedTransform.parent;
            // GameObject pedestrianClone = Instantiate(gameObject, cachedTransform.position + groupOffset, Quaternion.identity, cachedTransform.parent);
            //            pedestrianClone.name += i.ToString();
            pedestrianClone.name = $"{gameObject.name}_group_num_{i}_tiles_{tile.x}_{tile.y}"; // Better naming

            Pedestrian pedestrianScript = pedestrianClone.GetComponent<Pedestrian>();
            pedestrianScript.groupSpawned = true;

            /* pedestrianScript.ActivateFromPool(
           cachedTransform.position + groupOffset,
           currentStartNodePoint,
           tile
       );*/
            Debug.Log("Setting pathfinding variables for:" + pedestrianClone.name);
            pedestrianScript.path = new List<Vector3>(path);
            pedestrianScript.currentTargetDestination = path[0];

            pedestrianScript.pathListIndex = 0;
            pedestrianScript.groupSpawned = true;
            pedestrianScript.SpawnGroup = false;
            pedestrianScript.tile = tile;
            float waitTime = (i + 1) * 1.5f;
            pedestrianScript.groupIndividualWaitTime = Time.time + waitTime;
            pedestrianScript.endPoint = currentEndTargetDestination.Position;
            pedestrianScript.firstPathFindCalled = true;

            //new code
            //Set group-specific properties directly
            pedestrianScript.currentStartNodePoint = currentStartNodePoint;


            groupWithSamePath.Add(pedestrianScript);
            // FIX: Manually call initialization to create the DOTS entity and sync the path
            pedestrianScript.InitializePathfinding(); // <--- CRITICAL FIX LINE
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
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Draw current target
        if (firstPathFindCalled && currentTargetDestination != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentTargetDestination, 0.5f);
            Gizmos.DrawLine(transform.position, currentTargetDestination);
        }

        // Draw entire path
        Gizmos.color = Color.blue;
        for (int i = 0; i < path.Count; i++)
        {
            Gizmos.DrawSphere(path[i], 0.3f);
            if (i < path.Count - 1)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }

        // Draw target with offset
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetWithOffset, 0.4f);
    }
    public float GetMoveSpeed()
    {
        return moveSpeed;
    }
    public void ResetForPool()
    {
        //todo check if this needs to happen
        // 1. Cancel any ongoing tasks
        pathfindingCancellationTokenSource?.Cancel();
        pathfindingCancellationTokenSource?.Dispose();
        pathfindingCancellationTokenSource = null;

        // 2. Reset core navigation state
        firstPathFindCalled = false;
        findingPath = false;
        pathListIndex = 0;
        pathFoundCount = 0;

        tile = new Vector2Int(-1, -1);
        //path
        path.Clear();
        groupNextPath.Clear();
        currentTargetDestination = Vector3.zero;
        targetWithOffset = Vector3.zero;
        currentRightOffset = localRightDirectionOffsetStrength;
        // 5. Reset spawn/group state
        groupSpawned = false;
        SpawnGroup = false;  // TODO WHAT TO DO
        isEndPointOnSpawnSet = false;
        endPoint = Vector3.zero;

        // 6. Reset navigation nodes
        currentStartNodePoint = null;
        currentEndTargetDestination = null;
        // 7. Reset ragdoll if exists
        if (GetComponent<Ragdoll>() != null)
        {
            Ragdoll ragdoll = GetComponent<Ragdoll>();
            ragdoll.ResetRagdoll();

        }
        //tileManagerOffset?
        //group
        groupWithSamePath.Clear();
        getPositionNodePointTransformNeighbourPoints.Clear();

    }
    // Call this when taking pedestrian from pool
    public void ActivateFromPool(Vector3 spawnPosition, NodePoint startNode, Vector2Int spawnTile)
    {
        ResetForPool();

        // Set initial state
        SetPositionAndTeleport(spawnPosition);

        cachedTransform.position = spawnPosition;
        cachedTransform.rotation = Quaternion.identity;
        tile = spawnTile;
        currentStartNodePoint = startNode;

        // Set random values (moved from Start)
        randomGroupNumber = UnityEngine.Random.Range(1, maxGroupNumber);
        float randomSpeedOffset = UnityEngine.Random.Range(randomSpeedMinimumOffset, randomSpeedMaximumOffset);
        moveSpeed = 7f + randomSpeedOffset; // Base + random

        gameObject.SetActive(true);
    }
    private void SetPositionAndTeleport(Vector3 newPosition)
    {
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        cachedTransform.position = newPosition;

        if (characterController != null)
        {
            // Physics.SyncTransforms is often required after direct transform manipulation,
            // especially before re-enabling CharacterController to ensure physics is up-to-date.
//            Physics.SyncTransforms();
            characterController.enabled = true;
  //          Physics.SyncTransforms();
        }
        
        
    }

}