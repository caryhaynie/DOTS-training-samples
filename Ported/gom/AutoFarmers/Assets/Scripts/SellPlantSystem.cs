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
    [RequireComponentTag(typeof(SellPlantIntention))]
    struct SellPlantJob : IJobForEachWithEntity<TargetEntity, HoldingPlant>
    {
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        public void Execute(Entity entity,
            int index,
            [ReadOnly] ref TargetEntity store,
            [ReadOnly] ref HoldingPlant plant)
        {
            EntityCommandBuffer.RemoveComponent<SellPlantIntention>(index, entity);
            EntityCommandBuffer.RemoveComponent<HoldingPlant>(index, entity);
            EntityCommandBuffer.RemoveComponent<TargetEntity>(index, entity);
            EntityCommandBuffer.AddComponent<NeedGoal>(index, entity);

            // Re-parent plant to store
            EntityCommandBuffer.SetComponent(index, plant.Value, new Parent { Value = store.Value });
            EntityCommandBuffer.SetComponent(index, plant.Value, new Translation { Value = new float3(.5f, 1, .5f) });
            EntityCommandBuffer.AddComponent<PlantDeath>(index, plant.Value);
            EntityCommandBuffer.AddComponent(index, plant.Value, new NonUniformScale { Value = new float3(1) });
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