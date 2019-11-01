using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(SimulateFarmGroup))]
public class HarvestPlantSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    [ExcludeComponent(typeof(PathIndex))]
    [RequireComponentTag(typeof(HarvestPlantIntention))]
    struct HarvestPlantSystemJob : IJobForEachWithEntity<TargetEntity>
    {
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        public void Execute(Entity entity,
            int index,
            ref TargetEntity target)
        {

            EntityCommandBuffer.AddComponent<SellPlantIntention>(index, entity);
            EntityCommandBuffer.AddComponent<NeedPath>(index, entity);
            EntityCommandBuffer.AddComponent<HoldingPlant>(index, entity);
            EntityCommandBuffer.RemoveComponent<HarvestPlantIntention>(index, entity);

            // Parent plant to the farmer
            var plantEntity = target.Value;
            EntityCommandBuffer.AddComponent(index, plantEntity, new Parent { Value = entity });
            EntityCommandBuffer.SetComponent(index, plantEntity, new Translation { Value = new float3(0) });
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new HarvestPlantSystemJob
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDependencies);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;
    }
}