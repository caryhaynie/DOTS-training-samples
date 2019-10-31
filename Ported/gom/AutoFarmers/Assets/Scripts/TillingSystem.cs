using Unity.Entities;
using Unity.Jobs;
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
        }
    }

    private EndSimulationEntityCommandBufferSystem m_ECBSystem;

    protected override void OnCreate()
    {
        m_ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new MarkTilesAsTilledJob {
            ECB = m_ECBSystem.CreateCommandBuffer().ToConcurrent(),
            TilledPrefab = GetSingleton<PrefabManager>().TilledPrefab
        }.Schedule(this, inputDeps);
        m_ECBSystem.AddJobHandleForProducer(job);
        return job;
    }
}