using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class DecalManager : MonoBehaviour
{
    public static DecalManager Instance;

    [Header("Decal Settings")]
    public GameObject decalPrefab;
    public int poolDefaultCapacity = 30;
    public int poolMaxSize = 50;
    public float decalOffset = 0.01f; //position offset to remove jittering or something like that

    private ObjectPool<GameObject> decalPool;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        decalPool = new ObjectPool<GameObject>(
            createFunc: CreateDecal,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnToPool,
            actionOnDestroy: Destroy,
            collectionCheck: true,
            defaultCapacity: poolDefaultCapacity,
            maxSize:poolMaxSize
        );
    }

    private GameObject CreateDecal()
    {
        var d = Instantiate(decalPrefab);
        d.SetActive(false);
        return d;
    }

    private void OnTakeFromPool(GameObject decal)
    {
        // Defensive: if pool stored a destroyed object, create a fresh one
        if (decal == null)
        {
            decal = CreateDecal();
            if (decal == null) return;
        }
        decal.SetActive(true);
    }

    private void OnReturnToPool(GameObject decal)
    {
        if (decal == null) return;

        decal.SetActive(false);
    }

    public void SpawnDecal(Vector3 position, Vector3 normal, Transform parent = null)
    {
        GameObject decal = null;
        try
        {
            decal = decalPool.Get();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"DecalManager: pool.Get() failed: {e}. Falling back to CreateDecal().");
            decal = CreateDecal();
            if (decal != null) decal.SetActive(true);
        }
        decal.transform.SetParent(parent, worldPositionStays: true);

        decal.transform.position = position + normal * decalOffset;
       // decal.transform.rotation = Quaternion.LookRotation(normal);
        // Compute rotation so quad faces outwards, is flipped 180° on X, and has a random spin around normal
        float randomSpin = Random.Range(0f, 360f);
        Quaternion baseRot = Quaternion.LookRotation(normal);              // align +Z to surface normal
        Quaternion flipX = Quaternion.Euler(180f, 0f, 0f);                 // flip on X-axis
        Quaternion spin = Quaternion.AngleAxis(randomSpin, normal);        // spin around the hit normal

        // Order matters: base → flip → spin
        decal.transform.rotation = spin * (baseRot * flipX);


        // Optional: return to pool later if you want "rolling limit"
        StartCoroutine(ReturnDecalAfterLifetime(decal, 15f));
    }

    private IEnumerator ReturnDecalAfterLifetime(GameObject decal, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (decal == null) yield break;
        if (!decal.activeInHierarchy) yield break;
        if (decal != null)
            decalPool.Release(decal);
    }
}