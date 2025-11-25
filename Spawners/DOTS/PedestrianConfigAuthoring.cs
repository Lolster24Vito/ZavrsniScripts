using Unity.Entities;
using UnityEngine;

public class PedestrianConfigAuthoring : MonoBehaviour
{
    public GameObject PedestrianPrefab;
    public GameObject CarPrefab;
    public GameObject PlayerGhostPrefab; //This is added so that the DOTS subscene has a physics collider for interacting with pedestrians
    public GameObject BulletGhostPrefab; 

    class Baker : Baker<PedestrianConfigAuthoring>
    {
        public override void Bake(PedestrianConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PedestrianConfig
            {
                PedestrianPrefab = GetEntity(authoring.PedestrianPrefab, TransformUsageFlags.Dynamic),
                CarPrefab = GetEntity(authoring.CarPrefab, TransformUsageFlags.Dynamic),
                PlayerGhostPrefab = GetEntity(authoring.PlayerGhostPrefab, TransformUsageFlags.Dynamic),
                BulletGhostPrefab = GetEntity(authoring.BulletGhostPrefab, TransformUsageFlags.Dynamic) 
            });
        }
    }
}

public struct PedestrianConfig : IComponentData
{
    public Entity PedestrianPrefab;
    public Entity CarPrefab;
    public Entity PlayerGhostPrefab;
    public Entity BulletGhostPrefab; 
}