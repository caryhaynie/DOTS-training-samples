using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(UpdateMapSystemGroup))]
public class RockMapSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    public NativeArray<Entity> RockMap;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnDestroy() => RockMap.Dispose();

    struct CreateRockDataJob : IJobForEachWithEntity<RockDimensions, Translation>
    {
        public int Width;

        [NativeDisableParallelForRestriction]
        [WriteOnly]
        public NativeArray<Entity> Rocks;

        public void Execute(
            Entity entity,
            int index,
            [ReadOnly] ref RockDimensions dimensions,
            [ReadOnly] ref Translation position)
        {
            var tile = new int2(math.floor(position.Value.xz));
            var startIndex = tile.y * Width + tile.x;
            var rockWidth = dimensions.Value.x;
            var rockHeight = dimensions.Value.y;
            for (var y = 0; y < rockHeight; ++y)
            {
                var endIndex = startIndex + rockWidth;
                for (var i = startIndex; i < endIndex; ++i)
                {
                    Rocks[i] = entity;
                }
                startIndex += Width;
            }
        }
    }

    struct InitJob : IJob
    {
        [WriteOnly]
        public NativeArray<Entity> Rocks;

        public unsafe void Execute()
        {
            UnsafeUtility.MemClear(Rocks.GetUnsafePtr(), Rocks.Length * UnsafeUtility.SizeOf<Entity>());
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var mapData = GetSingleton<MapData>();
        var initJob = new InitJob
        {
            Rocks = RockMap
        }.Schedule(inputDeps);

        var createJob = new CreateRockDataJob
        {
            Width = mapData.Width,
            Rocks = RockMap
        }.Schedule(this, initJob);
        createJob.Complete();

        return inputDeps;
    }
}
