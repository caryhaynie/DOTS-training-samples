using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(SimulateFarmGroup))]
public class RockSmashSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    [BurstCompile]
    [RequireComponentTag(typeof(SmashRockIntention))]
    [ExcludeComponent(typeof(PathElement))]
    struct BuildSmashArray : IJobForEachWithEntity<TargetEntity>
    {
        public NativeArray<Entity> AttackedRocks;
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;
        [ReadOnly]
        public ComponentDataFromEntity<RockHealth> RockHealths;

        public void Execute(Entity entity, int index, ref TargetEntity targetRock)
        {
            if (!RockHealths.HasComponent(targetRock.Value))
            {
                EntityCommandBuffer.RemoveComponent<SmashRockIntention>(index, entity);
                EntityCommandBuffer.RemoveComponent<TargetEntity>(index, entity);
                EntityCommandBuffer.AddComponent<NeedGoal>(index, entity);
            }
            else
                AttackedRocks[index] = targetRock.Value;
        }
    }

    // Don't Burst because a rock can be attacked multiple times, and we don't want write collisions
    struct DecrementRockHealth : IJob
    {
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> AttackedRocks;

        public ComponentDataFromEntity<RockHealth> RockHealths;
        public ComponentDataFromEntity<Translation> RockTranslations;
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        public void Execute()
        {
            foreach (var entity in AttackedRocks)
            {
                if (RockHealths.Exists(entity))
                {
                    var health = RockHealths[entity];
                    health.CurrentHealth -= 1;
                    RockHealths[entity] = new RockHealth { CurrentHealth = health.CurrentHealth, MaxHealth = health.MaxHealth };

                    // "Animate" the rock moving downward
                    var incomingTranslation = RockTranslations[entity];
                    RockTranslations[entity] = new Translation { Value = new float3(incomingTranslation.Value.x, -0.5f + ((float)(health.CurrentHealth) / health.MaxHealth), incomingTranslation.Value.z) };

                    if (health.CurrentHealth <= 0)
                    {
                        EntityCommandBuffer.DestroyEntity(0, entity);
                    }
                }
            }
        }
    }

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var SmashesQuery = GetEntityQuery(new EntityQueryDesc {
            None = new ComponentType[] { typeof(PathElement) },
            All = new ComponentType[] { typeof(SmashRockIntention), typeof(TargetEntity) }
        });
        
        NativeArray<Entity> EntityArray = new NativeArray<Entity>(SmashesQuery.CalculateEntityCount(), Allocator.TempJob);

        var BuildArrayJob = new BuildSmashArray
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            RockHealths = GetComponentDataFromEntity<RockHealth>(),
            AttackedRocks = EntityArray
        }.Schedule(this, inputDependencies);

        var HitRockJob = new DecrementRockHealth
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            RockHealths = GetComponentDataFromEntity<RockHealth>(),
            RockTranslations = GetComponentDataFromEntity<Translation>(),
            AttackedRocks = EntityArray
        }.Schedule(BuildArrayJob);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(HitRockJob);

        // Now that the job is set up, schedule it to be run.
        return HitRockJob;
    }
}
