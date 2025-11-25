using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.AI.DOTS.Components
{
    public struct PedestrianTag : IComponentData { }

    public struct PedestrianData : IComponentData
    {
        public float MoveSpeed;
        public float RotationSpeed;
        public float RightOffset;
        public float MinDistanceForCompletion;
        public int PathIndex;
        public bool FirstPathFindCalled;
        public bool GroupSpawned;
        public float GroupWaitTime;
        public EntityType NpcType; 
    }

    public struct PathElement : IBufferElementData
    {
        public float3 Position;
    }
}