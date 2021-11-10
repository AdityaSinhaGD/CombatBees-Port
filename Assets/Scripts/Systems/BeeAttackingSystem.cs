using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(MovementSystem))]
public class BeeAttackingSystem : SystemBase
{
    private EntityCommandBufferSystem m_CommandBufferSystem;

    protected override void OnCreate()
    {
        m_CommandBufferSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var deltaTime = Time.DeltaTime;

        BattleField battlefield = GetSingleton<BattleField>();

        Entities.WithAll<Attack>()
                .ForEach( ( int entityInQueryIndex, Entity bee, ref Velocity velocity, ref TargetPosition targetPosition, in Translation translation, in TargetEntity targetEntity) =>
                {
                    //if target is not in a state to be attacked transition to default state..
                    if (HasComponent<Dying>(targetEntity.Value) || HasComponent<Destroy>(targetEntity.Value) || !HasComponent<Rotation>(targetEntity.Value))
                    {
                        ecb.RemoveComponent<Attack>(entityInQueryIndex, bee);
    
                        ecb.AddComponent<Default>(entityInQueryIndex, bee);
                    }
                    else
                    {
                        //Make the bee move towards the target entity
                        Translation targetEntityTranslationComponent = GetComponent<Translation>(targetEntity.Value);
                        float3 direction = targetEntityTranslationComponent.Value - translation.Value;
                        targetPosition.Value = targetEntityTranslationComponent.Value;

                        //If the bee is close enough, kill the other bee
                        float distanceToEnemyBee = math.length(direction);
                        if (distanceToEnemyBee < 1)
                        {
                       
                            if(HasComponent<IsCarryingResource>(targetEntity.Value))
                            {
                                ecb.RemoveComponent<IsCarryingResource>(entityInQueryIndex, targetEntity.Value);
                                var carryingComponentFromEnemy = GetComponent<IsCarryingResource>(targetEntity.Value);

                         

                                float3 hivePosition;
                                float hiveDistance = battlefield.HiveDistance + 1f;

                          
                                if (HasComponent<TeamA>(bee))
                                    hivePosition = new float3(0, 0, -hiveDistance);
                                else
                                    hivePosition = new float3(0, 0, hiveDistance);

                                ecb.RemoveComponent<TargetEntity>(entityInQueryIndex, bee);
                                ecb.RemoveComponent<Attack>(entityInQueryIndex, bee);
                                ecb.AddComponent<IsCarryingResource>(entityInQueryIndex, bee, new IsCarryingResource { Value = carryingComponentFromEnemy.Value });
                                ecb.SetComponent<TargetPosition>(entityInQueryIndex, bee, new TargetPosition { Value = hivePosition });
                            }
                            else
                            {
                            
                                ecb.RemoveComponent<TargetEntity>(entityInQueryIndex, bee);
                                ecb.RemoveComponent<Attack>(entityInQueryIndex, bee);
                                ecb.AddComponent<Default>(entityInQueryIndex, bee);
                                ecb.RemoveComponent<TargetEntity>(entityInQueryIndex, bee);
                   
                            }
                        
                            ecb.AddComponent<Dying>(entityInQueryIndex, targetEntity.Value);
                      
                            if(HasComponent<Default>(targetEntity.Value))
                                ecb.RemoveComponent<Default>(entityInQueryIndex, targetEntity.Value);
                            if(HasComponent<Attack>(targetEntity.Value))
                                ecb.RemoveComponent<Attack>(entityInQueryIndex, targetEntity.Value);
                            if(HasComponent<IsCarryingResource>(targetEntity.Value))
                                ecb.RemoveComponent<IsCarryingResource>(entityInQueryIndex, targetEntity.Value);
                            if(HasComponent<Collect>(targetEntity.Value))
                                ecb.RemoveComponent<Collect>(entityInQueryIndex, targetEntity.Value);
                    }
                    else if( distanceToEnemyBee < 10 )
                    {
                        //Move towards target
                        velocity.Value += direction * deltaTime * 20;
                    }
                }
                
            } ).ScheduleParallel();

            m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
