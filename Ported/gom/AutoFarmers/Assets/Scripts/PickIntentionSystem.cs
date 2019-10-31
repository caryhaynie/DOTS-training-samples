using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(SimulateFarmGroup))]
public class PickIntentionSystem : JobComponentSystem
{

    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }


    //[BurstCompile]
    //[RequireComponentTag(typeof(HoldingPlant))]
    //struct PickIntentionSellPlantJob : IJobForEachWithEntity<NeedGoal>
    //{
    //    public EntityCommandBuffer.Concurrent EntityCommandBuffer;

    //    public void Execute(Entity entity,
    //        int index,
    //        ref NeedGoal g)
    //    {
    //        EntityCommandBuffer.AddComponent<SellPlantIntention>(index, entity);
    //        EntityCommandBuffer.AddComponent<NeedPath>(index, entity);
    //        EntityCommandBuffer.RemoveComponent<NeedGoal>(index, entity);
    //    }
    //}

    [BurstCompile]
    //[ExcludeComponent(typeof(HoldingPlant))]
    struct PickIntentionJob : IJobForEachWithEntity<NeedGoal> // could instead try IJobChunk or IJobParallelFor
    {

        public EntityCommandBuffer.Concurrent EntityCommandBuffer;
        public Unity.Mathematics.Random random;


        public void Execute(Entity entity,
            int index,
            [ReadOnly] ref NeedGoal g)
        {

            int randomNum = random.NextInt(0, 4); // not inclusive

            if (randomNum == 0)
                EntityCommandBuffer.AddComponent<SmashRockIntention>(index, entity);
            if (randomNum == 1)
                EntityCommandBuffer.AddComponent<TillGroundIntention>(index, entity);
            if (randomNum == 2)
                EntityCommandBuffer.AddComponent<PlantSeedIntention>(index, entity);
            if (randomNum == 3)
                EntityCommandBuffer.AddComponent<HarvestPlantIntention>(index, entity);

            EntityCommandBuffer.AddComponent<NeedPath>(index, entity);
            EntityCommandBuffer.RemoveComponent<NeedGoal>(index, entity);


        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        //var pickIntentionSellPlant = new PickIntentionSellPlantJob
        //{
        //    EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()

        //}.Schedule(this, inputDeps);

        var pickIntention = new PickIntentionJob
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            random = new Unity.Mathematics.Random(1234)

        }.Schedule(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(pickIntention);

        return pickIntention;
    }
}