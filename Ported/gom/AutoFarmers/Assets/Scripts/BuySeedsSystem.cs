using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class BuySeedsSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    [ExcludeComponent(new[] { typeof(PathIndex), typeof(HasSeeds) })]
    [RequireComponentTag(new[] { typeof(PlantSeedIntention) })]
    struct BuySeedsJob : IJobForEachWithEntity<Translation, TargetEntity>
    {
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        public void Execute(Entity entity,
            int index,
            ref Translation position,
            ref TargetEntity target)
        {
            EntityCommandBuffer.RemoveComponent<TargetEntity>(index, entity);
            EntityCommandBuffer.AddComponent<NeedPath>(index, entity);
            EntityCommandBuffer.AddComponent<HasSeeds>(index, entity);

        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var buySeedsJob = new BuySeedsJob
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDependencies);


        m_EntityCommandBufferSystem.AddJobHandleForProducer(buySeedsJob);

        return buySeedsJob;
    }
}