using Unity.Entities;

public class PlayerNoBakeBaker : Baker<PlayerNoBakeAuthoring>
{
    public override void Bake(PlayerNoBakeAuthoring authoring)
    {
        // Get the entity Unity wants to create for this GameObject
        var entity = GetEntity(TransformUsageFlags.None);

        // "BakingOnlyEntity" tag tells Unity: 
        // "Use this for baking logic if needed, but DO NOT create a runtime entity for it."
        // This effectively strips it from the ECS world, leaving the GameObject alone.
        AddComponent<BakingOnlyEntity>(entity);
    }
}