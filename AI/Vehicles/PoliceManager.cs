using UnityEngine;
using UnityEngine.AddressableAssets;

public class PoliceManager : MonoBehaviour
{
    public static PoliceManager Instance;
    [SerializeField] private AssetReference policeOfficerPrefab;
    private GameObject loadedPolicePrefab;

    void Awake() { Instance = this; LoadPoliceAsset(); }

    async void LoadPoliceAsset()
    {
        var handle = policeOfficerPrefab.LoadAssetAsync<GameObject>();
        await handle.Task;
        loadedPolicePrefab = handle.Result;
    }

    public void SpawnPolicePursuit(Vector3 position)
    {
        if (loadedPolicePrefab == null) return;

        Transform player = TileManager.Instance.player;
        // Spawn 2 officers near the car
        for (int i = 0; i < 2; i++)
        {
            Vector3 spawnPos = position + (Random.insideUnitSphere * 3f);
            spawnPos.y = position.y; // Keep on ground

            GameObject cop = Instantiate(loadedPolicePrefab, spawnPos, Quaternion.identity);
            PoliceAI ai = cop.GetComponent<PoliceAI>();
            if (ai != null)
            {
                ai.SetTarget(player);
            }
            // Setup AI (You would have a 'PoliceAI' script that finds the player)
        }
        Debug.Log("POLICE DISPATCHED!");
    }
}