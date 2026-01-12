using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets; 
using UnityEngine.ResourceManagement.AsyncOperations; 
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public class NpcPoolManager : MonoBehaviour
{
    public static NpcPoolManager Instance { get; private set; }


    private ObjectPool<GameObject> pedestrianPool;
    private ObjectPool<GameObject> carPool;
    private ObjectPool<GameObject> ragdollPedestrianPool;


    //the "Loaded" prefab is stored here once it's loaded
    private GameObject loadedPedestrianPrefab;
    private GameObject loadedCarPrefab;
    private GameObject loadedRagdollPrefab;

    [Header("Pedestrian Settings")]
    [SerializeField] private AssetReference pedestrianRef;
    [SerializeField] private int pedestrianPoolDefaultCapacity = 30;
    [SerializeField] private int pedestrianPoolMaxSize = 50;
    [Header("Car Settings")]
    [SerializeField] private AssetReference carRef;
    [SerializeField] private int carPoolDefaultCapacity = 15;
    [SerializeField] private int carPoolMaxSize = 30;

    [Header("Ragdoll Settings")]
    [SerializeField] private AssetReference ragdollRef;
    [SerializeField] private int ragdollPoolDefaultCapacity = 5;
    [SerializeField] private int ragdollPoolMaxSize = 25;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            StartCoroutine(SetupAddressablePools());
            //            DontDestroyOnLoad(gameObject);
            //InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnDestroy()
    {
        if (pedestrianRef.IsValid()) pedestrianRef.ReleaseAsset();
        if (carRef.IsValid()) carRef.ReleaseAsset();
        if (ragdollRef.IsValid()) ragdollRef.ReleaseAsset();
    }

    IEnumerator SetupAddressablePools()
    {
        // 1. Load the Prefabs into RAM asynchronously
        AsyncOperationHandle<GameObject> pedOp = pedestrianRef.LoadAssetAsync<GameObject>();
        AsyncOperationHandle<GameObject> carOp = carRef.LoadAssetAsync<GameObject>();
        AsyncOperationHandle<GameObject> ragOp = ragdollRef.LoadAssetAsync<GameObject>();

        yield return pedOp;
        yield return carOp;
        yield return ragOp;

        if (pedOp.Status == AsyncOperationStatus.Succeeded)
        {
            loadedPedestrianPrefab = pedOp.Result;
            loadedCarPrefab = carOp.Result;
            loadedRagdollPrefab = ragOp.Result;

            // 2. ONLY NOW initialize the pools
            InitializePools();
        }
    }

    private void InitializePools()
    {
        // Initialize the pools
        pedestrianPool = new ObjectPool<GameObject>(
            createFunc: CreatePedestrian,
            actionOnGet: OnTakeFromPedestrianPool,
            actionOnRelease: OnReturnToPedestrianPool,
            actionOnDestroy: Destroy,
            collectionCheck: true,
            defaultCapacity: pedestrianPoolDefaultCapacity,
            maxSize: pedestrianPoolMaxSize
        );

        carPool = new ObjectPool<GameObject>(
            createFunc: CreateCar,
            actionOnGet: OnTakeFromCarPool,
            actionOnRelease: OnReturnToCarPool,
            actionOnDestroy: Destroy,
            collectionCheck: true,
            defaultCapacity: carPoolDefaultCapacity,
            maxSize: carPoolMaxSize
        );

        ragdollPedestrianPool = new ObjectPool<GameObject>(
            createFunc: CreateRagdollPedestrian,
            actionOnGet: OnTakeFromRagdollPedestrianPool,
            actionOnRelease: OnReturnToRagdollPedestrianPool,
            actionOnDestroy: Destroy,
            collectionCheck: true,
            defaultCapacity: ragdollPoolDefaultCapacity,
            maxSize: ragdollPoolMaxSize
        );

    }

    //GET METHODS
    public GameObject GetPedestrian()
    {
        return pedestrianPool.Get();
    }

    public GameObject GetRagdollPedestrian()
    {
        return ragdollPedestrianPool.Get();
    }
    public GameObject GetCar()
    {
        return carPool.Get();
    }

    //RELEASE METHODS
    public void ReleasePedestrian(GameObject obj)
    {
        pedestrianPool.Release(obj);
    }

    public void ReleaseCar(GameObject obj)
    {
        carPool.Release(obj);
    }
    public void ReleaseRagdollPedestrian(GameObject obj)
    {
        ragdollPedestrianPool.Release(obj);
    }
    //CREATE METHODS
    private GameObject CreatePedestrian()
    {
        GameObject spawnedPedestrian = Instantiate(loadedPedestrianPrefab, transform);
        spawnedPedestrian.SetActive(false);
        return spawnedPedestrian;
        //ON GET spawnedObject.name = $"{entityType.ToString()}_{i}_{currentTile.x}_{currentTile.y}";
        //Pedestrian pedestrianScript = spawnedObject.GetComponent<Pedestrian>();
        //pedestrianScript.SetTile(currentTile);
        //pedestrianScript.SetStartingNode(randomPosition);
    }
    private GameObject CreateCar()
    {
        GameObject car = Instantiate(loadedCarPrefab);
        car.SetActive(false);
        return car;
    }
    private GameObject CreateRagdollPedestrian()
    {
        GameObject ragdoll = Instantiate(loadedRagdollPrefab, transform);
        ragdoll.SetActive(false);
        return ragdoll;
    }

    //ON TAKE METHODS
    private void OnTakeFromPedestrianPool(GameObject pedestrian)
    {
        if (pedestrian == null)
        {
            return;
        }
        //        ActivateFromPool()
        pedestrian.SetActive(true);
    }
    private void OnTakeFromCarPool(GameObject car)
    {
        if (car == null) return;
        car.SetActive(true);
    }
    private void OnTakeFromRagdollPedestrianPool(GameObject ragdoll)
    {
        if (ragdoll == null) return;
        ragdoll.SetActive(true);
    }
    //On Return To Methods
    private void OnReturnToPedestrianPool(GameObject pedestrian)
    {
        if (pedestrian == null) return;
        // Reset state (e.g., stop movement, reset health)
        pedestrian.transform.SetParent(transform);
        pedestrian.GetComponent<Pedestrian>().ResetForPool();
        pedestrian.SetActive(false);
    }
    private void OnReturnToCarPool(GameObject car)
    {
        if (car == null) return;
        // Reset state
        car.GetComponent<Pedestrian>().ResetForPool();
        car.SetActive(false);
    }
    private void OnReturnToRagdollPedestrianPool(GameObject ragdoll)
    {
        if (ragdoll == null) return;
        // Reset ragdoll state
        Ragdoll ragdollScript = ragdoll.GetComponent<Ragdoll>();
        if (ragdollScript != null)
        {
            ragdollScript.ResetRagdoll();
        }
        ragdoll.SetActive(false);
    }
}
