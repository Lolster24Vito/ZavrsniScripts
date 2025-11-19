using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;
using AI.DOTS.Components;
using Unity.Transforms; // Required for LocalTransform
using Unity.Mathematics;

public class Pedestrian : MonoBehaviour
{
    public EntityType entityType;
    public float currentRightOffset = 0f;
    [SerializeField] private float localRightDirectionOffsetStrength = 0f;
    [SerializeField] private float localRightOffsetNpcCollision = 8f;

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
    [SerializeField] private Vector2Int tile = new Vector2Int(-1, -1);
    private Transform cachedTransform;
    private Vector3 targetWithOffsetWithPlayerY;

    public static int npcCount;

    [SerializeField] private float pathfindingCooldown = 1f;
    private float lastPathfindingTime = 0f;

    private CharacterController characterController;

    [Header("DOTS Hybrid")]
    public bool useDOTSMovement = true; // Toggle in Inspector
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
        
        characterController = GetComponent<CharacterController>();
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
    private void LateUpdate()
    {
        // If DOTS is moving the entity, we must manually sync the GameObject 
        // to follow it, because Runtime-Instantiated entities don't have automatic sync.
        if (useDOTSMovement)
        {
            SyncGameObjectToDots();
        }
    }
    // --- ADDED: SYNC VISUALS FROM DOTS TO GAME OBJECT ---
    private void SyncGameObjectToDots()
    {
        var authoring = GetComponent<PedestrianAuthoring>();
        if (authoring == null || authoring.BakedEntity == Entity.Null) return;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!em.Exists(authoring.BakedEntity)) return;

        if (em.HasComponent<LocalTransform>(authoring.BakedEntity))
        {
            var transformData = em.GetComponentData<LocalTransform>(authoring.BakedEntity);
            cachedTransform.position = transformData.Position;
            cachedTransform.rotation = transformData.Rotation;
        }
    }
    private void SetToNewestRecenterOffset(Vector3 offset)
    {
        currentWorldRecenterOffset -= offset;

        // FIX: If using DOTS, we must offset the Entity's position AND its path points.
        // If we don't do this, the visual sync in LateUpdate will snap the GameObject 
        // back to the old (non-offset) position.
        if (useDOTSMovement)
        {
            var authoring = GetComponent<PedestrianAuthoring>();
            if (authoring != null && authoring.BakedEntity != Entity.Null)
            {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (em.Exists(authoring.BakedEntity))
                {
                    // 1. Offset the Entity Position
                    if (em.HasComponent<LocalTransform>(authoring.BakedEntity))
                    {
                        var trans = em.GetComponentData<LocalTransform>(authoring.BakedEntity);
                        trans.Position -= (float3)offset; // Apply offset to DOTS position
                        em.SetComponentData(authoring.BakedEntity, trans);
                    }

                    // 2. Offset the Path Buffer (otherwise they walk back to the old coordinates)
                    if (em.HasBuffer<PathElement>(authoring.BakedEntity))
                    {
                        var buffer = em.GetBuffer<PathElement>(authoring.BakedEntity);
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            PathElement p = buffer[i];
                            p.Position -= (float3)offset;
                            buffer[i] = p;
                        }
                    }
                }
            }
        }
    }

    private Vector3 tileManagerOffset;

    public Vector3 getCurrentTarget()
    {
        return targetWithOffset;
    }
    protected virtual void FixedUpdate()
    {
        //this code might be faulty
        // === 2. DOTS MOVEMENT: Let DOTS move the entity ===


        if (!firstPathFindCalled && !findingPath && PedestrianDestinations.Instance.IsPathFindingReady())
        {
            if (PedestrianDestinations.Instance.IsPathFindingReady())
            {
                InitializePathfinding();
            }
            return;
        }
        bool isPathFinished = false;

        if (useDOTSMovement)
        {
            // If using DOTS, we must check the entity's state.
            // This is slow, but the simplest hybrid fix.
            isPathFinished = IsDotsPathFinished();
        }
        else
        {
            isPathFinished = (pathListIndex >= path.Count && path.Count > 0);
            // Fallback: Mono movement (for testing)
            // Continue to path logic below
        }
        if (isPathFinished && !findingPath)
        {
            HandlePathEndAndRefill(); // This calls FindPath() -> SyncPathToDots()
        }
        if (useDOTSMovement)
        {
            // DOTS movement system 
            return;
        }
        else
        {
            MoveTowardsTarget();

        }
        //UpdatePathProgressAndRefill();

    }

    private bool IsDotsPathFinished()
    {
        var authoring = GetComponent<PedestrianAuthoring>();
        if (authoring == null || authoring.BakedEntity == Entity.Null) return false;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!em.Exists(authoring.BakedEntity)) return false;

        try
        {
            // This is a "sync point" and is slow, but it's the simplest way.
            // This is likely your 20 FPS culprit if called 50x per frame.
            // We only do it if firstPathFindCalled is true.
            if (firstPathFindCalled &&
                em.HasComponent<PedestrianData>(authoring.BakedEntity) &&
                em.HasBuffer<PathElement>(authoring.BakedEntity))
            {
                var pedData = em.GetComponentData<PedestrianData>(authoring.BakedEntity);
                var buffer = em.GetBuffer<PathElement>(authoring.BakedEntity);

                // Check if the DOTS path index is at or beyond the buffer length
                return (buffer.Length > 0 && pedData.PathIndex >= buffer.Length);
            }
        }
        catch (Exception)
        {
            return false; // Entity might be destroying
        }

        return false; // Not ready to check
    }

    private void HandlePathEndAndRefill()
    {

        if (groupSpawned && groupNextPath.Count > 1)
        {
            path.Clear();
            path.AddRange(groupNextPath);
            //    cachedTransform.position = path[0];
            SetPositionAndTeleport(path[0]); // MODIFIED
            groupNextPath.Clear();
            pathListIndex = 0;
            return;
        }
        if (pathFoundCount > 5)
        {
            currentStartNodePoint = PedestrianDestinations.Instance.GetRandomNodePoint(entityType, tile);
            // cachedTransform.position = currentStartNodePoint.Position;
            SetPositionAndTeleport(currentStartNodePoint.Position); // MODIFIED

            currentEndTargetDestination = PedestrianDestinations.Instance.GetRandomNodePoint(entityType, tile);
            Debug.Log($"VITO object {gameObject.name} found path over 5 times");
            findingPath = true;
            FindPath();
            pathFoundCount = 0;
        }
        else
        {
            pathFoundCount++;
            findingPath = true;
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
          //  cachedTransform.position = currentStartNodePoint.Position;
            SetPositionAndTeleport(currentStartNodePoint.Position); // MODIFIED

        }
        Debug.Log("Now really started pedestrian: " + gameObject.name);
        if (useDOTSMovement)
        {
            EnsureDotsEntityCreated();
        }

        if (isEndPointOnSpawnSet)
        {
            currentEndTargetDestination = GetPositionNodePoint(endPoint);

        }
        else
        {
            currentEndTargetDestination = PedestrianDestinations.Instance.GetRandomNodePoint(entityType);
        }
        if (!groupSpawned)
        {
            findingPath = true;
            FindPath();

        }
        else
        {
            // If groupSpawned is true, the leader has already populated the 'path' list.
            // We just need to sync it to DOTS and ensure the state is correct.
            currentTargetDestination = path.Count > 0 ? path[0] : cachedTransform.position;
            firstPathFindCalled = true;
            findingPath = false;
            if (useDOTSMovement)
            {
                EnsureDotsEntityCreated();
                SyncPathToDots();

            }
        }
        //14_11 VITO todo old code  FindPath();


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
                returnNodePoint = PedestrianDestinations.Instance.GetRandomNodePoint(entityType, tile);

            }
        }
        return returnNodePoint;
    }

    protected async void FindPath()
    {   
        // Check if pathfinding is ready
        if (!PedestrianDestinations.Instance.IsPathFindingReady())
        {
            Debug.LogWarning("Pathfinding system is not ready yet");
            return;
        }
        pathfindingCancellationTokenSource?.Cancel(); // Cancel any ongoing pathfinding task
        pathfindingCancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = pathfindingCancellationTokenSource.Token;
        if (currentStartNodePoint == null) currentStartNodePoint = GetPositionNodePoint(cachedTransform.position);

        currentEndTargetDestination = PedestrianDestinations.Instance.GetRandomNodePoint(entityType);
        try
        {
            Debug.Log("Gameobject is finding path:" + gameObject.name);
            path = await PedestrianDestinations.Instance.FindPathAsync(currentStartNodePoint, currentEndTargetDestination, entityType, token);
            pathListIndex = 0;
            currentTargetDestination = path.Count > 0 ? path[0] : cachedTransform.position;
            firstPathFindCalled = true;
            findingPath = false;
            // === DOTS INTEGRATION: WRITE PATH TO ENTITY BUFFER ===
            SyncPathToDots();
            // === END DOTS WRITE ===
            // Spawn group after path calculation
            bool isFirstSpawn = false;
            if (SpawnGroup)
            {
                SpawnGroup = false;
                SpawnGroupWithOffset(randomGroupNumber);
                isFirstSpawn = true;
            }
            pathFoundCount++;

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

    private void SyncPathToDots()
    {
        var authoring = GetComponent<PedestrianAuthoring>();
        if (authoring == null)
        {
            Debug.LogError("PedestrianAuthoring component not found");
            return;
        }
        EnsureDotsEntityCreated();

        if (authoring.BakedEntity == Entity.Null)
        {
            Debug.LogError("BakedEntity is null even after creation attempt.");
            return;
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!em.Exists(authoring.BakedEntity))
        {
            Debug.LogError("Entity does not exist in EntityManager");
            return;
        }

        if (em.HasBuffer<PathElement>(authoring.BakedEntity))
        {
            var buffer = em.GetBuffer<PathElement>(authoring.BakedEntity);
            buffer.Clear();
            foreach (var pos in path)
                buffer.Add(new PathElement { Position = pos });

            if (em.HasComponent<PedestrianData>(authoring.BakedEntity))
            {
                PedestrianData data = em.GetComponentData<PedestrianData>(authoring.BakedEntity);
                data.PathIndex = 0;
                em.SetComponentData(authoring.BakedEntity, data);
            }
        }
        else
        {
            Debug.LogError("Entity does not have PathElement buffer");
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
            Physics.SyncTransforms();
            characterController.enabled = true;
            Physics.SyncTransforms();
        }
        if (useDOTSMovement)
        {
            var authoring = GetComponent<PedestrianAuthoring>();
            if (authoring != null && authoring.BakedEntity != Entity.Null)
            {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (em.Exists(authoring.BakedEntity))
                {
                    // Update Position
                    if (em.HasComponent<LocalTransform>(authoring.BakedEntity))
                    {
                        var trans = em.GetComponentData<LocalTransform>(authoring.BakedEntity);
                        trans.Position = (float3)newPosition;
                        trans.Rotation = cachedTransform.rotation; // Sync rotation too just in case
                        em.SetComponentData(authoring.BakedEntity, trans);
                    }

                    // CRITICAL: Reset the Path Buffer or the entity might try to walk back 
                    // to a path node that is now far away.
                    // Usually when teleporting, you want to clear the current path.
                    /* if (em.HasBuffer<PathElement>(authoring.BakedEntity))
                    {
                        var buffer = em.GetBuffer<PathElement>(authoring.BakedEntity);
                        // Optional: buffer.Clear(); 
                    }
                    */
                }
            }
        }
    }
    private void EnsureDotsEntityCreated()
    {
        var authoring = GetComponent<PedestrianAuthoring>();
        if (authoring == null) return;

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        // If entity already exists (e.g. from Baking), do nothing.
        if (authoring.BakedEntity != Entity.Null && em.Exists(authoring.BakedEntity)) return;

        // Create Entity Manually for Runtime Object
        Entity entity = em.CreateEntity();

        // Add Components matching your Baker
        em.AddComponent<PedestrianTag>(entity);
        em.AddComponent<PedestrianData>(entity);
        em.AddComponent<LocalTransform>(entity);
        em.AddBuffer<PathElement>(entity);

        // Initialize Data
        em.SetComponentData(entity, LocalTransform.FromPositionRotation(cachedTransform.position, cachedTransform.rotation));
        em.SetComponentData(entity, new PedestrianData
        {
            MoveSpeed = moveSpeed, // Use the randomized speed
            RotationSpeed = rotationSpeed,
            MinDistanceForCompletion = minDistanceForCompletion,
            PathIndex = 0
        });

        // Link it back so other methods find it
        authoring.BakedEntity = entity;
    }
}
