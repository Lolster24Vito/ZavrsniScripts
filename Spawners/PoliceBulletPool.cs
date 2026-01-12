using UnityEngine;
using UnityEngine.Pool;

public class PoliceBulletPool : MonoBehaviour
{
    public static PoliceBulletPool Instance;
    [SerializeField] private GameObject bulletPrefab;
    private ObjectPool<GameObject> pool;

    void Awake()
    {
        Instance = this;
        pool = new ObjectPool<GameObject>(
            createFunc: () => {
                GameObject obj = Instantiate(bulletPrefab);
                RegularBullet bScript = obj.GetComponent<RegularBullet>();

                if (bScript != null)
                {
                    bScript.onBulletDie = (b) => pool.Release(b);
                }
                else
                {
                    Debug.LogError($"PoliceBulletPool: {bulletPrefab.name} is missing RegularBullet component!");
                }
                return obj;
            },
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            maxSize: 100
        );
    }

    public GameObject GetBullet() => pool.Get();
}