using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(SimulateFarmGroup))]
public class SellPlantSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    [ExcludeComponent(typeof(PathIndex))]
    [RequireComponentTag(typeof(SellPlantIntention), typeof(HoldingPlant))]
    struct SellPlantJob : IJobForEachWithEntity<TargetEntity>
    {
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        public void Execute(Entity entity,
            int index,
            ref TargetEntity target)
        {
            EntityCommandBuffer.RemoveComponent<SellPlantIntention>(index, entity);
            EntityCommandBuffer.RemoveComponent<HoldingPlant>(index, entity);
            EntityCommandBuffer.RemoveComponent<TargetEntity>(index, entity);
            EntityCommandBuffer.AddComponent<NeedGoal>(index, entity);

            // do we need to un-parent plant to the farmer?
            // EntityCommandBuffer.DestroyEntity(index, target.Value); // is target the store or a plant?
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new SellPlantJob
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDependencies);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;
    }
}