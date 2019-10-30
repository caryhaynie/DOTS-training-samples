using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class HarvestPlantSystem : JobComponentSystem
{

    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }


    [BurstCompile]
    [ExcludeComponent(typeof(PathIndex))]
    struct HarvestPlantSystemJob : IJobForEachWithEntity<HarvestPlantIntention, TargetEntity>
    {
        // Add fields here that your job needs to do its work.
        // For example,
        //    public float deltaTime;
        
        
        
        public void Execute(Entity entity,
            int index, 
            ref TargetEntity target)
        {

            EntityCommandBuffer.AddComponent<SellPlantIntention>(index, entity);
            EntityCommandBuffer.AddComponent<NeedPath>(index, entity);
            EntityCommandBuffer.RemoveComponent<HarvestPlantIntention>(index, entity);

            // TODO parent plant to entity (TargetEntity)


        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new HarvestPlantSystemJob();
        
        // Assign values to the fields on your job here, so that it has
        // everything it needs to do its work when it runs later.
        // For example,
        //     job.deltaTime = UnityEngine.Time.deltaTime;
        
        
        
        // Now that the job is set up, schedule it to be run. 
        return job.Schedule(this, inputDependencies);
    }
}