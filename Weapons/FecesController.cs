using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class FecesController : MonoBehaviour
{
    public Transform leftHandAnchor;         // controller/hand transform (aim source)
    public Transform rightHandAnchor;         // controller/hand transform (aim source)

    [SerializeField] private LineRenderer leftHandLineRenderer;
    [SerializeField] private LineRenderer rightHandLineRenderer;
    [SerializeField] private LineRenderer bothHandLineRenderer;

    float leftDownTimerPress = 0f;
    float rightDownTimerPress = 0f;
    float bothDownTimerPress = 0f;

    [Header("Projectile / Pool")]
    public Transform bulletSpawnPoint;         // stomach/leg spawn transform
    public GameObject bulletPrefab;      // prefab with Rigidbody + Collider
    [SerializeField] private int poolDefaultCapacity = 10;
    [SerializeField] private int poolMaxSize = 50;
    public GameObject bothPreview;
    public Color previewTint = new Color(1f, 0.6f, 0.2f, 1f);
 //   private GameObject leftPreview, rightPreview, bothPreview;
    [Header("Scaling")]
    private Vector3 minSize = new Vector3(0.1f, 0.1f, 0.1f);
    private Vector3 maxSize = new Vector3(0.6f, 0.6f, 0.6f);
    private ObjectPool<GameObject> m_Pool;
    [SerializeField] private float maxHoldTime = 2.0f;

    [Header("Bullet settings")]
    [SerializeField] private float defaultBulletSpeed = 4f;
    [SerializeField] private float aimRange = 300f;

    private void Awake()
    {
        m_Pool = new ObjectPool<GameObject>(
                   createFunc: CreatePooledProjectile,
                   actionOnGet: OnTakeFromPool,
                   actionOnRelease: OnReturnToPool,
                   actionOnDestroy: OnDestroyPooledProjectile,
                   collectionCheck: true,
                   defaultCapacity: poolDefaultCapacity,
                   maxSize: poolMaxSize
               );
    }
    // Start is called before the first frame update
    private void Start()
    {
        DisableLineRenderers();
        // Make sure inspector-assigned preview GameObjects (if any) are deactivated at runtime.
        if (bothPreview != null) bothPreview.SetActive(false);

    }
    private void OnEnable()
    {
        //todo this should be in a seperate method for a single event
        //new start
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedDown += StartCharge;
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedUp += ReleaseCharge;

        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedDown += StartCharge;
        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedUp += ReleaseCharge;


        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedDown += StartCharge;
        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedUp += ReleaseCharge;
        //new end
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedDown += EnableAimingLineRenderer;
      //  GameEventsManager.Instance.inputEvents.onBothShootButtonPressedDown += StartCharge;
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedUp += DisableAimingLineRenderer;
     //   GameEventsManager.Instance.inputEvents.onBothShootButtonPressedUp += Shoot;


        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedDown += EnableAimingLineRenderer;
       // GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedDown += StartCharge;
        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedUp += DisableAimingLineRenderer;
   //     GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedUp += Shoot;

        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedDown += EnableAimingLineRenderer;
       // GameEventsManager.Instance.inputEvents.onRightShootButtonPressedDown += StartCharge;
        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedUp += DisableAimingLineRenderer;
   //     GameEventsManager.Instance.inputEvents.onRightShootButtonPressedUp += Shoot;
        //14.9.2025 17:30 working version backup
        /* GameEventsManager.Instance.inputEvents.onBothShootButtonPressedDown += EnableAimingLineRenderer;
          GameEventsManager.Instance.inputEvents.onBothShootButtonPressedDown += StartTimer;
          GameEventsManager.Instance.inputEvents.onBothShootButtonPressedUp += DisableAimingLineRenderer;
          GameEventsManager.Instance.inputEvents.onBothShootButtonPressedUp += Shoot;


          GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedDown += EnableAimingLineRenderer;
          GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedDown += StartTimer;
          GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedUp += DisableAimingLineRenderer;
          GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedUp += Shoot;

          GameEventsManager.Instance.inputEvents.onRightShootButtonPressedDown += EnableAimingLineRenderer;
          GameEventsManager.Instance.inputEvents.onRightShootButtonPressedDown += StartTimer;
          GameEventsManager.Instance.inputEvents.onRightShootButtonPressedUp += DisableAimingLineRenderer;
          GameEventsManager.Instance.inputEvents.onRightShootButtonPressedUp += Shoot;*/
    }

    private void OnDisable()
    {
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedDown -= StartCharge;
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedUp -= ReleaseCharge;

        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedDown -= StartCharge;
        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedUp -= ReleaseCharge;


        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedDown -= StartCharge;
        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedUp -= ReleaseCharge;
        //new end
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedDown -= EnableAimingLineRenderer;
        //  GameEventsManager.Instance.inputEvents.onBothShootButtonPressedDown += StartCharge;
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedUp -= DisableAimingLineRenderer;
        //   GameEventsManager.Instance.inputEvents.onBothShootButtonPressedUp += Shoot;


        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedDown -= EnableAimingLineRenderer;
        // GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedDown += StartCharge;
        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedUp -= DisableAimingLineRenderer;
        //     GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedUp += Shoot;

        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedDown -= EnableAimingLineRenderer;
        // GameEventsManager.Instance.inputEvents.onRightShootButtonPressedDown += StartCharge;
        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedUp -= DisableAimingLineRenderer;


  /*      GameEventsManager.Instance.inputEvents.onBothShootButtonPressedDown -= EnableAimingLineRenderer;
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedDown -= StartCharge;
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedUp -= DisableAimingLineRenderer;
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedUp -= Shoot;


        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedDown -= EnableAimingLineRenderer;
        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedDown -= StartCharge;
        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedUp -= DisableAimingLineRenderer;
        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedUp -= Shoot;

        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedDown -= EnableAimingLineRenderer;
        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedDown -= StartCharge;
        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedUp -= DisableAimingLineRenderer;
        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedUp -= Shoot;

        //new end
     //   GameEventsManager.Instance.inputEvents.onBothShootButtonPressedDown -= StartCharge;
        GameEventsManager.Instance.inputEvents.onBothShootButtonPressedUp -= ReleaseCharge;

       // GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedDown -= StartCharge;
        GameEventsManager.Instance.inputEvents.onLeftShootButtonPressedUp -= ReleaseCharge;

        //GameEventsManager.Instance.inputEvents.onRightShootButtonPressedDown -= StartCharge;
        GameEventsManager.Instance.inputEvents.onRightShootButtonPressedUp -= ReleaseCharge;*/
        // release any previews still active
        DeactivatePreview( bothPreview);
    //new end
    }
    private void Update()
    {
       
      
        if (bothPreview != null && bothPreview.activeSelf)
        {
            float downTimePress = Mathf.Max(maxHoldTime, leftDownTimerPress, rightDownTimerPress);
            float t = Mathf.Clamp01((Time.time - downTimePress) / maxHoldTime);
            SetPreviewScale(bothPreview, Vector3.Lerp(minSize, maxSize, t));
            bothPreview.transform.SetPositionAndRotation(bulletSpawnPoint.position, Quaternion.LookRotation(-bulletSpawnPoint.up));
        }
    }

    private void StartCharge(ShootTypes shootType)
    {
        switch (shootType)
        {
            case ShootTypes.LEFT:
                if (leftDownTimerPress == 0f) leftDownTimerPress = Time.time; //to prevent multiple calls on single button down
               // ActivatePreview(leftPreview, leftHandAnchor);   // ENABLE left preview specifically
                ActivatePreviewBoth(bothPreview);

                break;
            case ShootTypes.RIGHT:
                if (rightDownTimerPress == 0f) rightDownTimerPress = Time.time;
               // ActivatePreview(rightPreview, rightHandAnchor);
               ActivatePreviewBoth(bothPreview);
                break;
            case ShootTypes.BOTH:
                if (rightDownTimerPress == 0f) bothDownTimerPress = Time.time;
                ActivatePreviewBoth(bothPreview);
                break;
        }
    }

    private void ReleaseCharge(ShootTypes type)
    {
        // compute hold time (safe even if timer was 0)
        float pressedTime = 0f;
        switch (type)
        {
            case ShootTypes.LEFT:
                pressedTime = leftDownTimerPress > 0f ? Time.time - leftDownTimerPress : 0f;
                leftDownTimerPress = 0f; // reset
                break;
            case ShootTypes.RIGHT:
                pressedTime = rightDownTimerPress > 0f ? Time.time - rightDownTimerPress : 0f;
                rightDownTimerPress = 0f;
                break;
            case ShootTypes.BOTH:
                pressedTime = bothDownTimerPress > 0f ? Time.time - bothDownTimerPress : 0f;
                bothDownTimerPress = 0f;
                break;
        }

        float t = Mathf.Clamp01(pressedTime / maxHoldTime);
        Vector3 finalScale = Vector3.Lerp(minSize, maxSize, t) *2f;

        Vector3 direction;
        Transform handAnchor = null;
        if (type == ShootTypes.LEFT) handAnchor = leftHandAnchor;
        else if (type == ShootTypes.RIGHT) handAnchor = rightHandAnchor;

        if (type == ShootTypes.BOTH) direction = -bulletSpawnPoint.up;
        else direction = GetAimDirectionFromHand(handAnchor);

        // disable preview (don't null the references)
        DeactivatePreview(bothPreview);

        SpawnAndInitProjectile(direction, finalScale, defaultBulletSpeed);
    }

    private void ActivatePreviewBoth(GameObject preview)
    {
        if (preview == null) return;
        preview.transform.SetPositionAndRotation(bulletSpawnPoint.position, Quaternion.LookRotation(-bulletSpawnPoint.up));
        SetPreviewScale(preview, minSize);
        preview.SetActive(true);
    }

    private void DeactivatePreview(GameObject preview)
    {
        if (preview == null) return;
        preview.SetActive(false);
    }
    private void SetPreviewScale(GameObject previewObj, Vector3 s)
    {
            previewObj.transform.localScale = s;
    }
    private void EnableAimingLineRenderer(ShootTypes shootTypes)
    {
        switch (shootTypes)
        {
            case ShootTypes.LEFT:
                leftHandLineRenderer.enabled = true;
                rightHandLineRenderer.enabled = false;
                bothHandLineRenderer.enabled = false;

                break;
            case ShootTypes.RIGHT:
                leftHandLineRenderer.enabled = false;
                rightHandLineRenderer.enabled = true;
                bothHandLineRenderer.enabled = false;
                break;
            case ShootTypes.BOTH:
                rightHandLineRenderer.enabled = false;
                rightHandLineRenderer.enabled = false;
                bothHandLineRenderer.enabled = true;
                break;
        }
    }
    private void DisableAimingLineRenderer(ShootTypes shootTypes)
    {
       
                leftHandLineRenderer.enabled = false;
                rightHandLineRenderer.enabled = false;
                bothHandLineRenderer.enabled = false;
         
    }
    public void Shoot(ShootTypes shootTypes)
    {
        if (bulletSpawnPoint == null)
        {
            Debug.LogWarning("FecesController: bulletSpawnPoint not set. Aborting Shoot.");
            return;
        }

        float pressedTime = 0f;
        switch (shootTypes)
        {
            case ShootTypes.LEFT:
                pressedTime = Time.time - leftDownTimerPress;
                break;
            case ShootTypes.RIGHT:
                pressedTime = Time.time - rightDownTimerPress;
                break;
            case ShootTypes.BOTH:
                pressedTime = Time.time - bothDownTimerPress;
                break;
        }
        // clamp and map to 0..1 for lerp (you can change maxHold = 2.0f)
        float t = Mathf.Clamp01(pressedTime / maxHoldTime);

        Vector3 finalScale = Vector3.Lerp(minSize, maxSize, t);

        Vector3 direction;
        Transform handAnchor = null;
        if (shootTypes == ShootTypes.LEFT) handAnchor = leftHandAnchor;
        else if (shootTypes == ShootTypes.RIGHT) handAnchor = rightHandAnchor;

        if (shootTypes == ShootTypes.BOTH)
        {
            direction = -bulletSpawnPoint.up;
        }
        else
        {
            direction = GetAimDirectionFromHand(handAnchor);
        }
        SpawnAndInitProjectile(direction, finalScale, defaultBulletSpeed);

    }



    void SpawnAndInitProjectile(Vector3 directionFromSpawn,Vector3 scale,float speed)
        {
        if (m_Pool == null)
        {
            Debug.LogError("FecesController: Pool not initialized.");
            return;
        }
        // guard direction
        if (directionFromSpawn.sqrMagnitude <= Mathf.Epsilon)
        {
            Debug.LogError("Vito in here epsilon SpawnInitProjectile");
            directionFromSpawn = bulletSpawnPoint.forward;
        }
        GameObject feces = m_Pool.Get();

        feces.transform.SetPositionAndRotation(bulletSpawnPoint.position, Quaternion.LookRotation(directionFromSpawn));
        feces.transform.localScale = scale;

       // feces.SetActive(true);

        FecesBullet fb = feces.GetComponent<FecesBullet>();
        // Set rotation to face travel direction
        if (fb != null)
        {
            fb.Initialize(bulletSpawnPoint.position, directionFromSpawn, speed);
        }
        else
        {
            Debug.LogWarning("Pooled object missing FecesBullet component. Releasing immediately.");
            m_Pool.Release(feces);
        }
        // Clamp and map hold time
       
        }
    
    //todo events on down activate line renderers
    private void DisableLineRenderers()
    {
        leftHandLineRenderer.enabled = false;
        rightHandLineRenderer.enabled = false;
        bothHandLineRenderer.enabled = false;
    }
    private Vector3 GetAimDirectionFromHand(Transform handAnchor)
    {
        if (handAnchor == null || bulletSpawnPoint == null) return Vector3.zero;
        float aimRange = 300f;
        Vector3 targetPoint = handAnchor.position + handAnchor.forward * aimRange;
        Vector3 dir = (targetPoint - bulletSpawnPoint.position);
        if (dir.sqrMagnitude <= 0.0001f)
        {
            // fallback to hand forward if spawn is exactly at the hand (rare)
            dir = handAnchor.forward;
        }

        return dir.normalized;
    }

    private GameObject CreatePooledProjectile()
    {
        GameObject projectile = Instantiate(bulletPrefab);

        projectile.SetActive(false);

        // Add a callback to the projectile so it can return itself to the pool
        FecesBullet fecesBullet = projectile.GetComponent<FecesBullet>();

        if (fecesBullet != null)
        {
            fecesBullet.onBulletDie = OnBulletDie;
        }
        return projectile;
    }



    private void OnTakeFromPool(GameObject projectile)
    {
        if (projectile == null) return;

        projectile.SetActive(true);
    }

    private void OnReturnToPool(GameObject projectile)
    {
        // Reset bullet state and physics so it comes back "clean"
        FecesBullet fecesBullet = projectile.GetComponent<FecesBullet>();
        if (fecesBullet != null)
        {
            fecesBullet.OnReturnedToPool();
        }
        projectile.SetActive(false);
    }

    private void OnDestroyPooledProjectile(GameObject projectile)
    {
        Destroy(projectile);
    }
    private void OnBulletDie(GameObject obj)
    {
        // Defensive: only release if pool exists and object is not null
        if (m_Pool != null && obj != null) m_Pool.Release(obj);
    }

}
public enum ShootTypes
{
    LEFT,RIGHT,BOTH
}
