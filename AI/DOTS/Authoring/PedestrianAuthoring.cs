using Unity.Entities;
using UnityEngine;
using AI.DOTS.Components;

public class PedestrianAuthoring : MonoBehaviour
{
    public float MoveSpeed = 7f;
    public float RotationSpeed = 20f;
    [HideInInspector] public Entity BakedEntity;
    public Vector3 InitialTargetPosition = new Vector3(1, 300, 1);
    public Vector3 InitialTargetPosition1 = new Vector3(100, 300, 300);
    private class Baker : Baker<PedestrianAuthoring>
    {
        public override void Bake(PedestrianAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            authoring.BakedEntity = entity; // LINK Mono to DOTS 

            AddComponent(entity, new PedestrianTag());

            AddComponent(entity, new PedestrianData
            {
                MoveSpeed = authoring.MoveSpeed,
                RotationSpeed = authoring.RotationSpeed,
                RightOffset = 0f,
                MinDistanceForCompletion = 1.5f,
                PathIndex = 0,
                FirstPathFindCalled = true, //return to false after testing
                GroupSpawned = false,
                GroupWaitTime = 0f
            });
            DynamicBuffer<PathElement> pathBuffer = AddBuffer<PathElement>(entity);
            pathBuffer.Add(new PathElement { Position = authoring.InitialTargetPosition });
            pathBuffer.Add(new PathElement { Position = authoring.InitialTargetPosition1 });

        }
    }
}
