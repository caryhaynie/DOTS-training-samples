using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public class TillingSystem : JobComponentSystem
{
    [RequireComponentTag(typeof(NeedsTilling))]
    struct MarkTilesAsTilledJob : IJobForEachWithEntity_EC<LandState>
    {
        public EntityCommandBuffer.Concurrent ECB;
        public Entity TilledPrefab;
        public void Execute(Entity entity, int jobIndex, ref LandState landState)
        {
            var tilled = ECB.Instantiate(jobIndex, TilledPrefab);
            ECB.AddComponent<Parent>(jobIndex, tilled, new Parent { Value = entity });
            ECB.SetComponent<Translation>(jobIndex, tilled, new Translation { Value = new float3(0.25f, 0.5f, 0.25f) });

            ECB.SetComponent<LandState>(jobIndex, entity, new LandState { Value = LandStateType.Tilled });
            ECB.RemoveComponent<NeedsTilling>(jobIndex, entity);
        }
    }

    [RequireComponentTag(typeof(TillGroundIntention))]
    [ExcludeComponent(typeof(PathElement))]
    struct BuildTillArray : IJobForEachWithEntity<TargetEntity>
    {
        public NativeArray<Entity> GroundToMarkToTill;
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        public void Execute(Entity entity, int index, ref TargetEntity target)
        {
            if (target.Value == Entity.Null)
            {
                EntityCommandBuffer.RemoveComponent<TillGroundIntention>(index, entity);
                EntityCommandBuffer.RemoveComponent<TargetEntity>(index, entity);
                EntityCommandBuffer.AddComponent<NeedGoal>(index, entity);
            }
            else
                GroundToMarkToTill[index] = target.Value;
        }
    }

    // Don't Burst because ground can be tilled by multiple farmers, and we don't want write collisions
    struct MarkToTill : IJob
    {
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> GroundToMarkToTill;
        
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        public void Execute()
        {
            foreach (var entity in GroundToMarkToTill)
            {
                EntityCommandBuffer.AddComponent<NeedsTilling>(0, entity);
            }
        }
    }

    private EndSimulationEntityCommandBufferSystem m_ECBSystem;

    protected override void OnCreate()
    {
        m_ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var ToMarkToTillQuery = GetEntityQuery(new EntityQueryDesc {
            None = new ComponentType[] { typeof(PathElement) },
            All = new ComponentType[] { typeof(TillGroundIntention), typeof(TargetEntity) }
        });
        
        NativeArray<Entity> EntityArray = new NativeArray<Entity>(ToMarkToTillQuery.CalculateEntityCount(), Allocator.TempJob);

        var MarkTilled = new MarkTilesAsTilledJob {
            ECB = m_ECBSystem.CreateCommandBuffer().ToConcurrent(),
            TilledPrefab = GetSingleton<PrefabManager>().TilledPrefab
        }.Schedule(this, inputDeps);

        var BuildArrayJob = new BuildTillArray
        {
            EntityCommandBuffer = m_ECBSystem.CreateCommandBuffer().ToConcurrent(),
            GroundToMarkToTill = EntityArray
        }.Schedule(this, MarkTilled);

        var MarkToTillJob = new MarkToTill 
        {
            EntityCommandBuffer = m_ECBSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(BuildArrayJob);

        m_ECBSystem.AddJobHandleForProducer(MarkToTillJob);
        return MarkToTillJob;
    }
}