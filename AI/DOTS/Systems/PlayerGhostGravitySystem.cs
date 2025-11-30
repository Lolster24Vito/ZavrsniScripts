using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

public partial class PlayerGhostGravitySystem : SystemBase
{
    protected override void OnUpdate()
    {
        float gravity = 9.81f;

        Entities.WithAll<PlayerGhostTag>().ForEach((ref PhysicsVelocity velocity) =>
        {
            velocity.Linear.y -= gravity * SystemAPI.Time.DeltaTime;
        }).ScheduleParallel();
    }
}