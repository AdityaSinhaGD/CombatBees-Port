using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


[UpdateInGroup(typeof(InitializationSystemGroup))]
public class BeeSpawningSystem : SystemBase
{
    private Random m_Random;
    private int randomRange = 10;
    
    protected override void OnCreate()
    {
        m_Random = new Random((uint)randomRange);
    }
    
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var random = new Random( (uint)m_Random.NextUInt() );

        Entities.ForEach((Entity spawnerEntity, in BeeSpawner spawner, in Translation spawnerTranslation) =>
        {
            bool isTeamA = HasComponent<TeamA>( spawnerEntity );

            Entity bee;

            if(isTeamA)
            {
                bee = spawner.BeePrefab_TeamA;
            }
            else
            {
                bee = spawner.BeePrefab_TeamB;
            }

           
            for( int i = 0; i < spawner.Count; ++i )
            {
                var instance = ecb.Instantiate(bee);
                ecb.SetComponent(instance, new Translation {Value = spawnerTranslation.Value + random.NextFloat3Direction()});
                ecb.AddComponent<Default>( instance );
                ecb.AddComponent<TargetPosition>( instance, new TargetPosition { Value = float3.zero } );
                
                ecb.SetComponent<Velocity>( instance, new Velocity{ Value = random.NextFloat3Direction() * 100 });
            }
            ecb.DestroyEntity(spawnerEntity);
        }).Run();
        
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}