using UnityEngine;
using System.Collections;

public class PoliceAI : MonoBehaviour
{
    [Header("Police Chase Logic")]
    private Transform targetPlayer;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float stoppingDistance = 25f;
    [SerializeField] private float fireRate = 1.2f;
    [SerializeField] private Transform shootPositionTr;
    private float nextFireTime;
    private CharacterController characterController;
    private Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
        characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        // Must subscribe to recentering so the AI doesn't get left behind when the world shifts 
        WorldRecenterManager.OnWorldRecentered += HandleWorldRecentered;
    }

    private void OnDisable()
    {
        WorldRecenterManager.OnWorldRecentered -= HandleWorldRecentered;
    }

     // Update position if the world coordinates shift 
    private void HandleWorldRecentered(Vector3 offset)
    {
        cachedTransform.position -= offset;
    }

    public void SetTarget(Transform target)
    {
        targetPlayer = target;
    }

    private void FixedUpdate()
    {
        if (targetPlayer == null) return;

        float dist = Vector3.Distance(cachedTransform.position, targetPlayer.position);

        if (dist > stoppingDistance)
        {
            MoveTowardsPlayer();
        }
        else
        {
            LookAtTarget();
            if (Time.time > nextFireTime)
            {
                FireBullet();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    private void MoveTowardsPlayer()
    {
        // Direct vector to the player [cite: 260]
        Vector3 direction = (targetPlayer.position - cachedTransform.position).normalized;
        direction.y = 0; // Keep movement on the ground plane [cite: 242]

        if (direction.sqrMagnitude > 0.001f)
        {
            // Rotation logic moved from Pedestrian [cite: 266]
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Movement logic: use CharacterController if available, otherwise direct transform 
            if (characterController != null && characterController.enabled)
            {
                characterController.SimpleMove(direction * moveSpeed);
            }
            else
            {
                cachedTransform.position += direction * moveSpeed * Time.deltaTime;
            }
        }
    }

    private void LookAtTarget()
    {
        Vector3 dir = (targetPlayer.position - cachedTransform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotationSpeed);
        }
    }

    private void FireBullet()
    {
        if (PoliceBulletPool.Instance == null) return;

        GameObject bObj = PoliceBulletPool.Instance.GetBullet();
        // Spawn slightly in front and up from the center [cite: 339]
        Vector3 spawnPos = shootPositionTr.position;
        Vector3 fireDir = (targetPlayer.position - spawnPos).normalized;

        RegularBullet bScript = bObj.GetComponent<RegularBullet>();
        if (bScript != null)
        {
            bScript.Initialize(spawnPos, fireDir, 25f);
        }
    }
}