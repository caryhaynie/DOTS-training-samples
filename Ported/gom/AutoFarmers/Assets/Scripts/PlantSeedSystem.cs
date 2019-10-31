﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class PlantSeedSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    // plant last seed in the path; TODO determine how we'll plant the other seeds WHILE moving, maybe a tag updated by the movement system?
    [BurstCompile]
    [ExcludeComponent(typeof(PathIndex))]
    [RequireComponentTag(new[] { typeof(PlantSeedIntention), typeof(HasSeeds)})] 
    struct PlantLastSeedJob : IJobForEachWithEntity<Translation, TargetEntity>
    {

        public EntityCommandBuffer.Concurrent EntityCommandBuffer;
        public EntityCommandBuffer.Concurrent CommandBuffer;


        public void Execute(Entity entity,
            int index,
            ref Translation position,
            ref TargetEntity target)
        {
            Entity newPlant = EntityCommandBuffer.CreateEntity(index);
            // Entity newPlant = EntityCommandBuffer.Instantiate(index, plantSpawner.Prefab); // TODO create a plant prefab and use it
            EntityCommandBuffer.AddComponent(index, newPlant, position); // TODO probably want this somewhere different on the tile (e.g. the center)
            EntityCommandBuffer.AddComponent<PlantGrowth>(index, newPlant);
            EntityCommandBuffer.SetComponent(index, newPlant, new Scale { Value = 0 }); 


            EntityCommandBuffer.RemoveComponent<TargetEntity>(index, entity);
            EntityCommandBuffer.RemoveComponent<PlantSeedIntention>(index, entity); 
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {

        var plantLastSeedJob = new PlantLastSeedJob
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDependencies);


        m_EntityCommandBufferSystem.AddJobHandleForProducer(plantLastSeedJob);

        return plantLastSeedJob;
    }
}