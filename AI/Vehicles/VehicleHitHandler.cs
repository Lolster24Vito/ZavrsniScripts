using UnityEngine;

public class VehicleHitHandler : MonoBehaviour
{
    public int hitCount = 0;
    public bool policeSpawned = false;

    public void RegisterHit()
    {
        hitCount++;
        if (hitCount >= 2 && !policeSpawned)
        {
            policeSpawned = true;
            PoliceManager.Instance.SpawnPolicePursuit(transform.position);
        }
    }
}