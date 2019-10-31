using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

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

        public void Execute(Entity entity, int index, ref TargetEntity targetRock)
        {
            if (targetRock.Value == Entity.Null)
            {
                EntityCommandBuffer.RemoveComponent<SmashRockIntention>(index, entity);
                EntityCommandBuffer.RemoveComponent<TargetEntity>(index, entity);
                EntityCommandBuffer.AddComponent<NeedGoal>(index, entity);
            }
            else
                AttackedRocks[index] = targetRock.Value;
        }
    }

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
            
            //ComponentType.ReadOnly<SmashRockIntention>(), ComponentType.ReadOnly<TargetEntity>(), ComponentType.ReadOnly<PathElement>());
        NativeArray<Entity> EntityArray = new NativeArray<Entity>(SmashesQuery.CalculateEntityCount(), Allocator.TempJob);

        var BuildArrayJob = new BuildSmashArray
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
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

    //[BurstCompile]
    //[ExcludeComponent(typeof(SmashRockIntentionSmashing))]
    //struct ChooseRockToSmashJob : IJobForEachWithEntity_EC<Translation>
    //{
    //    // Add fields here that your job needs to do its work.
    //    // For example,
    //    //    public float deltaTime;

    //    public EntityCommandBuffer.Concurrent EntityCommandBuffer;

    //    // These are disposed of later in the dispose jobs.  No need for [DeallocateOnJobCompletion]
    //    public NativeArray<Translation> TranslationArray;
    //    public NativeArray<RockDimensions> RockDimensionArray;
    //    public NativeArray<Entity> EntityArray;

    //    public void Execute(
    //        Entity entity,
    //        int index,
    //        [ReadOnly] ref Translation translation
    //        )
    //    {
    //        // Find rock in current position
    //        var FarmerPosition = math.floor(translation.Value);
    //        for(int i = 0; i < TranslationArray.Length; i++)
    //        {
    //            // TranslationArray represents rock positions (square closest to origin)
    //            // RockDimensionsArray represnts rock dimensions

    //            if (FarmerPosition.x >= TranslationArray[i].Value.x
    //                && FarmerPosition.x < (TranslationArray[i].Value.x + RockDimensionArray[i].Value.x)
    //                && FarmerPosition.y >= TranslationArray[i].Value.y
    //                && FarmerPosition.y < (TranslationArray[i].Value.y + RockDimensionArray[i].Value.y)
    //            )
    //            {
    //                // if rock entity has no AttackerList Component, add one
    //                EntityArray[i].has

    //                // add farmer entity to AttackerList

    //                // if rock has no BeingAttacked Component, add one

    //                // increment BeingAttacked.NumAttackers

    //                // Communicate that farmer is not smashing a rock
    //                EntityCommandBuffer.AddComponent<SmashRockIntentionSmashing>(index, entity);
    //                return;
    //            }
    //        }
    //    }
    //}

    //struct DisposeJob<T> : IJob where T : struct
    //{
    //    public NativeArray<T> arrayToDispose;

    //    public void Execute() {
    //        arrayToDispose.Dispose();
    //    }
    //}

    //[BurstCompile]
    //struct RockSmashSystemJob : IJobForEachWithEntity_EBCC<AttackerList, BeingAttacked, RockHealth>
    //{
    //    // Add fields here that your job needs to do its work.
    //    // For example,
    //    //    public float deltaTime;
    //    public EntityCommandBuffer.Concurrent EntityCommandBuffer;

    //    public void Execute(
    //        Entity entity,
    //        int index,
    //        DynamicBuffer<AttackerList> attackers,
    //        [ReadOnly] ref BeingAttacked attackInformation,
    //        ref RockHealth health
    //        )
    //    {
    //        health.Value -= attackInformation.NumAttackers;

    //        if (health.Value <= 0)
    //        {
    //            // Destroy the rock
    //            EntityCommandBuffer.DestroyEntity(index, entity);

    //            for (int i = 0; i < attackers.Length; i++)
    //            {
    //                // Set worker to have no goal
    //                EntityCommandBuffer.RemoveComponent<SmashRockIntention>(index, attackers[i].Value);
    //                EntityCommandBuffer.RemoveComponent<SmashRockIntentionSmashing>(index, attackers[i].Value);
    //            }
    //        }
    //    }
    //}

    //protected override void OnCreate()
    //{
    //    base.OnCreate();
    //    m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    //}

    //protected override JobHandle OnUpdate(JobHandle inputDependencies)
    //{
    //    var RockPositionQuery = GetEntityQuery(ComponentType.ReadOnly<RockDimensions>(), ComponentType.ReadOnly<Translation>());
    //    NativeArray<Translation> TranslationArray = RockPositionQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
    //    NativeArray<RockDimensions> RockDimensionArray = RockPositionQuery.ToComponentDataArray<RockDimensions>(Allocator.TempJob);
    //    NativeArray<Entity> EntityArray = RockPositionQuery.ToEntityArray(Allocator.TempJob);

    //    var ChooseRockJobHandle = new ChooseRockToSmashJob
    //    {
    //        EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
    //        TranslationArray = TranslationArray,
    //        RockDimensionArray = RockDimensionArray,
    //        EntityArray = EntityArray
    //    }.Schedule(this, inputDependencies);

    //    var SmashJobHandle = new RockSmashSystemJob
    //    {
    //        EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
    //    }.Schedule(this, ChooseRockJobHandle);

    //    var DisposeJob1 = new DisposeJob<Translation>
    //    {
    //        arrayToDispose = TranslationArray
    //    }.Schedule(ChooseRockJobHandle);

    //    var DisposeJob2 = new DisposeJob<RockDimensions>
    //    {
    //        arrayToDispose = RockDimensionArray
    //    }.Schedule(ChooseRockJobHandle);

    //    var DisposeJob3 = new DisposeJob<Entity>
    //    {
    //        arrayToDispose = EntityArray
    //    }.Schedule(ChooseRockJobHandle);

    //    // Combine all the job handles from this system so other systems can wait on this one if needed.
    //    var jobHandles = new NativeArray<JobHandle>(3, Allocator.Temp);
    //    jobHandles[0] = SmashJobHandle;
    //    jobHandles[1] = DisposeJob1;
    //    jobHandles[2] = DisposeJob2;
    //    jobHandles[3] = DisposeJob3;
    //    inputDependencies = JobHandle.CombineDependencies(jobHandles);

    //    m_EntityCommandBufferSystem.AddJobHandleForProducer(SmashJobHandle);

    //    // Now that the job is set up, schedule it to be run. 
    //    return inputDependencies;
    //}


}