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
    EntityQuery m_SellersQuery;
    int m_Money;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_SellersQuery = GetEntityQuery(new EntityQueryDesc
        {
            None = new ComponentType[] { typeof(PathIndex) },
            All = new ComponentType[] { typeof(SellPlantIntention), typeof(TargetEntity), typeof(HoldingPlant) }
        });
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

    struct SpawnFarmersJob : IJob
    {
        public Entity Prefab;
        public int Count;
        public EntityCommandBuffer EntityCommandBuffer;
        public float3 Position;

        public void Execute()
        {
            while (Count-- > 0)
            {
                var farmer = EntityCommandBuffer.Instantiate(Prefab);
                EntityCommandBuffer.SetComponent(farmer, new Translation{ Value = Position });
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new SellPlantJob
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDependencies);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        m_Money += m_SellersQuery.CalculateEntityCount();

        if (m_Money > 50)
        {
            var spawnHandle = new SpawnFarmersJob
            {
                Position = new float3(0),
                Prefab = GetSingleton<PrefabManager>().FarmerPrefab,
                Count = m_Money / 50,
                EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer()
            }.Schedule(job);
            m_Money -= (m_Money / 50) * 50;
            m_EntityCommandBufferSystem.AddJobHandleForProducer(spawnHandle);
            job = JobHandle.CombineDependencies(spawnHandle, job);
        }

        return job;
    }
}