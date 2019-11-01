using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public class TillingSystem : JobComponentSystem
{
    [ExcludeComponent(typeof(NeedPath), typeof(PathElement))]
    [RequireComponentTag(typeof(TillGroundIntention))]
    struct MarkTilesAsTilledJob : IJobForEachWithEntity<TargetEntity>
    {
        public EntityCommandBuffer.Concurrent ECB;
        public Entity TilledPrefab;

        public void Execute(Entity farmerEntity, int jobIndex, [ReadOnly] ref TargetEntity target)
        {
            var landEntity = target.Value;
            var tilledEntity = ECB.Instantiate(jobIndex, TilledPrefab);
            ECB.AddComponent<Parent>(jobIndex, tilledEntity, new Parent { Value = landEntity });
            ECB.AddComponent<LocalToParent>(jobIndex, tilledEntity);
            ECB.SetComponent<Translation>(jobIndex, tilledEntity, new Translation { Value = new float3(0f, 0f, 0f) });
            ECB.SetComponent<LandState>(jobIndex, landEntity, new LandState { Value = LandStateType.Tilled });
            ECB.RemoveComponent<TillGroundIntention>(jobIndex, farmerEntity);
            ECB.AddComponent<NeedGoal>(jobIndex, farmerEntity);
        }
    }

    EndSimulationEntityCommandBufferSystem m_ECBSystem;

    protected override void OnCreate()
    {
        m_ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var MarkTilled = new MarkTilesAsTilledJob {
            ECB = m_ECBSystem.CreateCommandBuffer().ToConcurrent(),
            TilledPrefab = GetSingleton<PrefabManager>().TilledPrefab
        }.Schedule(this, inputDeps);
        m_ECBSystem.AddJobHandleForProducer(MarkTilled);

        return MarkTilled;
    }
}