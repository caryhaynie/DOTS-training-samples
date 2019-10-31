using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class PlantGrowthSystem : JobComponentSystem
{
    public readonly static float growthScale = 0.1f;
    public readonly static float grownSize = 3f;

    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    [RequireComponentTag(typeof(PlantGrowth))]
    struct PlantGrowthJob : IJobForEachWithEntity<Scale>
    {
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        public void Execute(
            Entity entity,
            int index,
            ref Scale scale)
        {
            scale.Value += growthScale;

            if (scale.Value > grownSize)
            {
                EntityCommandBuffer.AddComponent<HarvestablePlant>(index, entity);
                EntityCommandBuffer.RemoveComponent<PlantGrowth>(index, entity);
            }

        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var growPlantJob = new PlantGrowthJob
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDependencies);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(growPlantJob);

        return growPlantJob;
    }
}