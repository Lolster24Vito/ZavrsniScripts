using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public class NpcPoolManager : MonoBehaviour
{
    public static NpcPoolManager Instance { get; private set; }


    private ObjectPool<GameObject> pedestrianPool;
    private ObjectPool<GameObject> carPool;
    [Header("Pedestrian Settings")]
    [SerializeField] private GameObject pedestrianPrefab;
    [SerializeField] private int pedestrianPoolDefaultCapacity = 30;
    [SerializeField] private int pedestrianPoolMaxSize = 50;
    [Header("Car Settings")]
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private int carPoolDefaultCapacity = 15;
    [SerializeField] private int carPoolMaxSize = 30;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
//            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
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
    }

    public GameObject GetPedestrian()
    {
        return pedestrianPool.Get();
    }

    public void ReleasePedestrian(GameObject obj)
    {
        pedestrianPool.Release(obj);
    }

    public GameObject GetCar()
    {
        return carPool.Get();
    }

    public void ReleaseCar(GameObject obj)
    {
        carPool.Release(obj);
    }

    private GameObject CreatePedestrian()
    {
        GameObject spawnedPedestrian = Instantiate(pedestrianPrefab);
        spawnedPedestrian.SetActive(false);
        return spawnedPedestrian;
        //ON GET spawnedObject.name = $"{entityType.ToString()}_{i}_{currentTile.x}_{currentTile.y}";
        //Pedestrian pedestrianScript = spawnedObject.GetComponent<Pedestrian>();
        //pedestrianScript.SetTile(currentTile);
        //pedestrianScript.SetStartingNode(randomPosition);
    }
    private GameObject CreateCar()
    {
        if (carPrefab == null)
        {
            Debug.LogError("PedestrianObjectPoolManager: Car Prefab is not assigned!");
            return null;
        }
        GameObject car = Instantiate(carPrefab);
        car.SetActive(false);
        return car;
    }
    private void OnTakeFromPedestrianPool(GameObject pedestrian)
    {
        if (pedestrian == null) return;
        pedestrian.SetActive(true);
    }
    private void OnTakeFromCarPool(GameObject car)
    {
        if (car == null) return;
        car.SetActive(true);
    }
    private void OnReturnToPedestrianPool(GameObject pedestrian)
    {
        if (pedestrian == null) return;
        // Reset state (e.g., stop movement, reset health)
       // pedestrian.GetComponent<Pedestrian>().ResetForPool();
       // pedestrian.SetActive(false);
    }
    private void OnReturnToCarPool(GameObject car)
    {
        if (car == null) return;
        // Reset state
    //    car.GetComponent<Pedestrian>().ResetForPool();
     //   car.SetActive(false);
    }
}
