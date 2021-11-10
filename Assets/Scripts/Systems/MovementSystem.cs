using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public class MovementSystem : SystemBase
{

    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;

        Dependency = Entities.WithNone<Dying>().ForEach((ref Velocity velocity, in Translation translation, in TargetPosition target, in Speed speed ) =>
        {
            float3 direction = target.Value - translation.Value;
           
            float currentSpeed = math.length( velocity.Value );
            float3 normalizedDirection = math.normalize( direction );

            velocity.Value += (normalizedDirection * speed.Acceleration) * deltaTime;
            
           
        }).ScheduleParallel( Dependency );

        Dependency = Entities.ForEach((ref Translation translation, ref Velocity velocity) =>
        {
            translation.Value += velocity.Value * deltaTime;
            
        }).ScheduleParallel( Dependency );
        
        // Rotate Entites to movement direction
        Dependency = Entities.WithNone<Resource>().ForEach((ref Translation translation, ref Velocity velocity, ref NonUniformScale nonUniformScale, ref Rotation rotation) =>
        {
            rotation.Value = quaternion.LookRotationSafe(velocity.Value, new float3(0, 1, 0));
            nonUniformScale.Value.z = math.length(velocity.Value) * 0.1f;
        }).ScheduleParallel( Dependency );

        
    }


}
