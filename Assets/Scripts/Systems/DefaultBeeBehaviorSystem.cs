using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;

public class DefaultBeeBehaviorSystem : SystemBase
{
	private EntityQuery Resources;
	private EntityQuery TeamABees;
	private EntityQuery TeamBBees;

	private Random m_Random;
	private EntityCommandBufferSystem m_ECBSystem;

	protected override void OnCreate()
	{
		Resources = GetEntityQuery( new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<Resource>(),
			},
			None = new[]
			{
				ComponentType.ReadOnly<Collected>(),
			}
		} );

		TeamABees = GetEntityQuery( new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<TeamA>(),
				ComponentType.ReadOnly<Bee>(),
			},
			None = new[]
			{
				ComponentType.ReadOnly<Dying>(),
				ComponentType.ReadOnly<Destroy>(),
			}
		} );

		TeamBBees = GetEntityQuery( new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadOnly<TeamB>(),
				ComponentType.ReadOnly<Bee>(),
			},
			None = new[]
			{
				ComponentType.ReadOnly<Dying>(),
				ComponentType.ReadOnly<Destroy>(),
			}
		} );

		m_Random = new Random( 7 );
		m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate()
	{
		var random = new Random( (uint) m_Random.NextInt() );

		int resourceEntitiesLength = Resources.CalculateEntityCount();
		var ecb = m_ECBSystem.CreateCommandBuffer();

		/*If there are no resources to collect then attack other bees with resources*/
		if( resourceEntitiesLength == 0 )
		{
			int teamABeesEntitiesLength = TeamABees.CalculateEntityCount();
			int teamBBeesEntitiesLength = TeamBBees.CalculateEntityCount();

			var beeEntities_TeamA =
				TeamABees.ToEntityArrayAsync( Allocator.TempJob, out var beeAEntitiesHandle );
			var beeEntities_TeamB =
				TeamBBees.ToEntityArrayAsync( Allocator.TempJob, out var beeBEntitiesHandle );

			Dependency = JobHandle.CombineDependencies( Dependency, beeAEntitiesHandle );
			Dependency = JobHandle.CombineDependencies( Dependency, beeBEntitiesHandle );

			// Give every idle bee a random target from the opposing team.
			Entities.WithAll<TeamA>()
				.WithDisposeOnCompletion( beeEntities_TeamB )
				.WithAll<Default>()
				.ForEach( ( Entity bee ) =>
				{
					int targetIndex = random.NextInt( 0, teamBBeesEntitiesLength );

					if( targetIndex < teamBBeesEntitiesLength )
					{
						ecb.RemoveComponent<Default>( bee );
						ecb.AddComponent<Attack>( bee );
						ecb.AddComponent( bee, new TargetEntity {Value = beeEntities_TeamB[targetIndex]} );
					}
				} ).Schedule();

			Entities.WithAll<TeamB>()
				.WithDisposeOnCompletion( beeEntities_TeamA )
				.WithAll<Default>()
				.ForEach( ( Entity bee ) =>
				{
					int targetIndex = random.NextInt( 0, teamABeesEntitiesLength );

					if( targetIndex < teamABeesEntitiesLength )
					{
						ecb.RemoveComponent<Default>( bee );
						ecb.AddComponent<Attack>( bee );
						ecb.AddComponent( bee, new TargetEntity {Value = beeEntities_TeamA[targetIndex]} );
					}
				} ).Schedule();
		}
		else
		{
			var resourceEntities =
				Resources.ToEntityArrayAsync( Allocator.TempJob, out var resourcesEntitiesHandle );
			Dependency = JobHandle.CombineDependencies( Dependency, resourcesEntitiesHandle );

			Entities.WithAll<Default>()
				.WithDisposeOnCompletion( resourceEntities )
				.ForEach( ( Entity bee ) =>
				{
					ecb.RemoveComponent<Default>( bee );
					ecb.AddComponent<Collect>( bee );

					int targetIndex = random.NextInt( 0, resourceEntitiesLength );
					ecb.AddComponent( bee, new TargetEntity {Value = resourceEntities[targetIndex]} );
				} ).Schedule();
		}

		m_ECBSystem.AddJobHandleForProducer( Dependency );
	}
}