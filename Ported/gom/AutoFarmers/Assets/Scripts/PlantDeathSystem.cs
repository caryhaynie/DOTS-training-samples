using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(SimulateFarmGroup))]
public class PlantDeathSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    [RequireComponentTag(typeof(PlantDeath))]
    struct PlantDeathJob : IJobForEachWithEntity<NonUniformScale, Translation>
    {
        public float DeltaTime;
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        public void Execute(
            Entity entity,
            int index,
            ref NonUniformScale scale,
            ref Translation position)
        {
            if (scale.Value.x > .1f)
            {
                scale.Value *= new float3(.9f, 1, .9f);
            }
            else if (position.Value.y > 10f)
            {
                EntityCommandBuffer.RemoveComponent<PlantDeath>(index, entity);
                EntityCommandBuffer.AddComponent<Disabled>(index, entity);
            }
            else
            {
                position.Value += math.up() * 5 * DeltaTime;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new PlantDeathJob
        {
            DeltaTime = UnityEngine.Time.deltaTime,
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDependencies);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;
    }
}