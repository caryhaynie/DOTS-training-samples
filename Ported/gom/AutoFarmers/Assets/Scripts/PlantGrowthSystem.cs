using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

public struct PlantGrowthSettings : IComponentData
{
    public float Scale;
    public float Size;
}

public struct NeedsPlantMeshGeneration : IComponentData {}

public class PlantGrowthSystem : JobComponentSystem
{
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
        public PlantGrowthSettings Settings;

        public void Execute(
            Entity entity,
            int index,
            ref Scale scale)
        {
            scale.Value += Settings.Scale;

            if (scale.Value > Settings.Size)
            {
                EntityCommandBuffer.AddComponent<HarvestablePlant>(index, entity);
                EntityCommandBuffer.RemoveComponent<PlantGrowth>(index, entity);
            }

        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {

        var settings = GetSingleton<PlantGrowthSettings>();

        var growPlantJob = new PlantGrowthJob
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            Settings = settings,
        }.Schedule(this, inputDependencies);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(growPlantJob);

        return growPlantJob;
    }
}