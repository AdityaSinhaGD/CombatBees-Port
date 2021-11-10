using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


[UpdateBefore(typeof(MovementSystem))]
[UpdateBefore(typeof(BeeAttackingSystem))]
public class BeeCollectingSystem : SystemBase
{
    private EntityCommandBufferSystem m_CommandBufferSystem;

    protected override void OnCreate()
    {
        m_CommandBufferSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        BattleField battlefield = GetSingleton<BattleField>();

        Entities.WithAll<Collect>()
                .ForEach( ( int entityInQueryIndex, Entity bee, ref TargetPosition targetPosition, in Translation translation, in TargetEntity targetEntity ) =>
            {
                // check if the resource has been destroyed // If resource has been taken by another bee
                if (!HasComponent<Translation>(targetEntity.Value) || HasComponent<Collected>(targetEntity.Value))
                {
                    ecb.RemoveComponent<Collect>(entityInQueryIndex, bee);
                    ecb.RemoveComponent<TargetEntity>(entityInQueryIndex, bee);
                    ecb.AddComponent<Default>(entityInQueryIndex, bee);
                    return;
                }

                //If the bee is close enough, change its state to Carrying
                Translation targetEntityTranslationComponent = GetComponent<Translation>( targetEntity.Value );
                targetPosition.Value = targetEntityTranslationComponent.Value;
                float distanceToResource = math.length(targetEntityTranslationComponent.Value - translation.Value);
                if (distanceToResource < 1)
                {
                   
                    ecb.AddComponent<Collected>(entityInQueryIndex, targetEntity.Value);
                    ecb.SetComponent<Translation>(entityInQueryIndex, targetEntity.Value, new Translation { Value = new float3(0, -1, 0) });

                    float hiveDistance = battlefield.HiveDistance + 10f;
                    float3 hivePosition = new float3(0, 0, hiveDistance);
                    if (HasComponent<TeamA>(bee))
                        hivePosition.z *= -1;

                    ecb.RemoveComponent<Collect>(entityInQueryIndex, bee);
                    ecb.RemoveComponent<TargetEntity>(entityInQueryIndex, bee);
                    ecb.SetComponent<TargetPosition>(entityInQueryIndex, bee, new TargetPosition { Value = hivePosition });
                    ecb.AddComponent<IsCarryingResource>(entityInQueryIndex, bee, new IsCarryingResource { Value = targetEntity.Value });
                }
                
            } ).ScheduleParallel();
    }
}
