using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class MovePathersSystem : JobComponentSystem
{
    float m_Speed = 1;

    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    public struct MoveJob : IJobForEachWithEntity_EBCC<PathElement, Translation, PathIndex>
    {
        // Speed * deltaTime
        public float DeltaPosition;

        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        public void Execute(
            Entity entity,
            int index,
            [ReadOnly] DynamicBuffer<PathElement> path,
            ref Translation position,
            ref PathIndex pathIndex)
        {
            var targetTile = path[pathIndex.Value].Value;
            var targetPosition = new float3(targetTile.x, 0, targetTile.y) + new float3(.5f, 0, .5f);
            var directionToTarget = targetPosition - position.Value;

            // Move farmer
            position.Value += normalize(directionToTarget) * DeltaPosition;

            // Check to see if we've reached the target tile
            var currentTile = math.floor(position.Value.xy);
            if (math.all(currentTile == targetTile))
            {
                if (pathIndex.Value < path.Length - 1)
                {
                    pathIndex.Value++;
                }
                else
                {
                    EntityCommandBuffer.RemoveComponent<PathElement>(index, entity);
                    EntityCommandBuffer.RemoveComponent<PathIndex>(index, entity);
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var moveHandle = new MoveJob
        {
            DeltaPosition = m_Speed * UnityEngine.Time.deltaTime,
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDependencies);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(moveHandle);

        return moveHandle;
    }
}
