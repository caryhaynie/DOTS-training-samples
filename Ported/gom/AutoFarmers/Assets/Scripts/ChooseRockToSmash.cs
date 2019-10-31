using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class ChooseRockToSmash : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new ChooseRockToSmashJob
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            RockPositionQuery = GetEntityQuery(ComponentType.ReadOnly<RockDimensions>(), ComponentType.ReadOnly<Translation>())
        }.Schedule(this, inputDependencies);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;
    }
}