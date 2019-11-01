﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
// using static Unity.Mathematics.math;

[UpdateInGroup(typeof(SimulateFarmGroup))]
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
        public Entity PlantPrefab;
        public Unity.Mathematics.Random Rng;

        public void Execute(Entity entity,
            int index,
            ref Translation position,
            ref TargetEntity target)
        {
            //Entity newPlant = EntityCommandBuffer.CreateEntity(index);
            Entity newPlant = EntityCommandBuffer.Instantiate(index, PlantPrefab);

            EntityCommandBuffer.AddComponent(index, newPlant, position); // TODO probably want this somewhere different on the tile (e.g. the center)
            EntityCommandBuffer.AddComponent<PlantGrowth>(index, newPlant);
            EntityCommandBuffer.AddComponent(index, newPlant, new Scale { Value = 0 });
            var rndRotation = quaternion.Euler(0, Rng.NextFloat(0, 2 * 3.141592f), 0);
            EntityCommandBuffer.SetComponent(index, newPlant, new Rotation { Value = rndRotation });


            EntityCommandBuffer.RemoveComponent<TargetEntity>(index, entity);
            EntityCommandBuffer.RemoveComponent<PlantSeedIntention>(index, entity);

        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {

        var plantLastSeedJob = new PlantLastSeedJob
        {
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            PlantPrefab = GetSingleton<PrefabManager>().PlantPrefab,
            Rng = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue)),
        }.Schedule(this, inputDependencies);


        m_EntityCommandBufferSystem.AddJobHandleForProducer(plantLastSeedJob);

        return plantLastSeedJob;
    }
}