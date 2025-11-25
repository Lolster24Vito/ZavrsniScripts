using Unity.Entities;
using UnityEngine;
using Assets.Scripts.AI.DOTS.Components;

public class PedestrianAuthoring : MonoBehaviour
{
    public float MoveSpeed = 7f;
    public float RotationSpeed = 20f;
    [HideInInspector] public Entity BakedEntity;
    public Vector3 InitialTargetPosition = new Vector3(1, 300, 1);
    public Vector3 InitialTargetPosition1 = new Vector3(100, 300, 300);
    public GameObject RagdollPrefab; // Reference to  Ragdoll Prefab
    public EntityType NpcType;
    private class Baker : Baker<PedestrianAuthoring>
    {
        public override void Bake(PedestrianAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            //authoring.BakedEntity = entity; // LINK Mono to DOTS 
            // 1. Basic Components
            AddComponent(entity, new PedestrianTag());
            AddComponent(entity, new PedestrianData
            {
                MoveSpeed = authoring.MoveSpeed,
                RotationSpeed = authoring.RotationSpeed,
                RightOffset = 0f,
                MinDistanceForCompletion = 1.5f,
                PathIndex = 0,
                NpcType = authoring.NpcType // <--- ADD THIS

                                            //todo VITO provjeri dali ovo sve zezne
                                            // FirstPathFindCalled = true, //return to false after testing
                                            //  GroupSpawned = false,
                                            //  GroupWaitTime = 0f
            });
            AddBuffer<PathElement>(entity);
            /* old code
            DynamicBuffer<PathElement> pathBuffer = AddBuffer<PathElement>(entity);
            pathBuffer.Add(new PathElement { Position = authoring.InitialTargetPosition });
            pathBuffer.Add(new PathElement { Position = authoring.InitialTargetPosition1 });
            */
        }
    }
}
// Add this component to store the specific ragdoll prefab for this unit type
public class RagdollPrefabReference : IComponentData
{
    public GameObject Prefab; // Managed component because it references a GameObject
}