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
    public NativeArray<Entity> StoreMap;

    public JobHandle Handle;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnDestroy()
    {
        if (RockMap.IsCreated) RockMap.Dispose();
        if (StoreMap.IsCreated) StoreMap.Dispose();
    }

    [BurstCompile]
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

    [BurstCompile]
    struct CreateStoreDataJob : IJobForEachWithEntity<Store, Translation>
    {
        public int Width;

        [NativeDisableParallelForRestriction]
        [WriteOnly]
        public NativeArray<Entity> Stores;

        public void Execute(
            Entity entity, int index,
            [ReadOnly] ref Store store,
            [ReadOnly] ref Translation position)
        {
            var tile = new int2(math.floor(position.Value.xz));
            var tileIndex = tile.y * Width + tile.x;
            Stores[tileIndex] = entity;
        }
    }

    JobHandle m_Handles;

    public void AddJobHandleForProducer(JobHandle jobHandle)
    {
        m_Handles = JobHandle.CombineDependencies(jobHandle, m_Handles);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        m_Handles.Complete();
        m_Handles = default;

        var mapData = GetSingleton<MapData>();
        if (RockMap.IsCreated) RockMap.Dispose();
        RockMap = new NativeArray<Entity>(mapData.Width * mapData.Height, Allocator.TempJob);

        if (StoreMap.IsCreated) StoreMap.Dispose();
        StoreMap = new NativeArray<Entity>(mapData.Width * mapData.Height, Allocator.TempJob);

        var rockHandle = new CreateRockDataJob
        {
            Width = mapData.Width,
            Rocks = RockMap
        }.Schedule(this, inputDeps);

        var storeHandle = new CreateStoreDataJob
        {
            Width = mapData.Width,
            Stores = StoreMap,
        }.Schedule(this, inputDeps);

        Handle = JobHandle.CombineDependencies(rockHandle, storeHandle);

        return Handle;
    }
}
