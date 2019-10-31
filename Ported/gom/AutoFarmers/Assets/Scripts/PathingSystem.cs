using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class PathingSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    [RequireComponentTag(typeof(HarvestablePlant))]
    struct CreatePlantDataJob : IJobForEach<Translation>
    {
        public int Width;

        public NativeArray<int> PlantCounts;

        public void Execute([ReadOnly] ref Translation position)
        {
            var tile = new int2(math.floor(position.Value.xz));
            var tileIndex = tile.y * Width + tile.x;
            PlantCounts[tileIndex] = 1;
        }
    }

    // [BurstCompile]
    struct CreateRockDataJob : IJobForEachWithEntity<RockDimensions, Translation>
    {
        public int Width;

        [NativeDisableContainerSafetyRestriction]
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
    struct CreateLandDataJob : IJobForEachWithEntity<Translation, LandState>
    {
        public int Width;
        public NativeArray<LandStateType> Land;
        public NativeArray<Entity> LandEntities;

        public void Execute(
            Entity entity,
            int index,
            [ReadOnly] ref Translation position,
            [ReadOnly] ref LandState landState)
        {
            var tile = new int2(math.floor(position.Value.xz));
            var tileIndex = tile.y * Width + tile.x;
            Land[tileIndex] = landState.Value;
            LandEntities[tileIndex] = entity;
        }
    }

    struct PQueue
    {
        int m_Capacity;
        int m_Length;

        public int Length => m_Length;
        public int Capacity => m_Capacity;
        NativeArray<int3> m_Data;

        public void Dispose() => m_Data.Dispose();

        public PQueue(int capacity, Allocator label)
        {
            m_Capacity = capacity;
            m_Length = 0;
            m_Data = new NativeArray<int3>(capacity, label, NativeArrayOptions.UninitializedMemory);
        }

        public void Enqueue(int2 position, int value)
        {
            // see if already in queue
            for (int i = 0; i < m_Length; ++i)
            {
                if (math.all(m_Data[i].xy == position))
                {
                    m_Data[i] = new int3(m_Data[i].xy, value);
                    return;
                }
            }

            m_Data[m_Length++] = new int3(position, value);
        }

        public int2 Dequeue()
        {
            int bestIndex = -1;
            int bestValue = 0;
            for (int i = 0; i < m_Length; ++i)
            {
                if (bestIndex == -1 || m_Data[i].z < bestValue)
                {
                    bestIndex = i;
                    bestValue = m_Data[i].z;
                }
            }

            var ret = m_Data[bestIndex];

            // Swap back
            if (m_Length > 1) m_Data[bestIndex] = m_Data[m_Length - 1];
            --m_Length;

            return ret.xy;
        }
    }

    struct Utils
    {
        public static unsafe void Init(int width, int height, float3 worldPosition, out NativeArray<int> distances, out PQueue queue, out int steps)
        {
            int mapSize = width * height;
            distances = new NativeArray<int>(mapSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var minValue = int.MinValue;
            UnsafeUtility.MemCpyReplicate(distances.GetUnsafePtr(), UnsafeUtility.AddressOf(ref minValue), 4, distances.Length);

            int2 currentTile = new int2(math.floor(worldPosition.xz));
            int currentIndex = currentTile.y * width + currentTile.x;
            steps = 0;
            distances[currentIndex] = 0;

            queue = new PQueue(mapSize, Allocator.TempJob);
            queue.Enqueue(currentTile, 0);
        }

        public static DynamicBuffer<PathElement> AddPathToEntity(EntityCommandBuffer.Concurrent entityCommandBuffer, int index, Entity entity)
        {
            entityCommandBuffer.AddComponent(index, entity, new PathIndex{ Value = 0 });
            return entityCommandBuffer.AddBuffer<PathElement>(index, entity);
        }

        public static int MarkVisitedAndGetNextDistance(NativeArray<int> distances, int index)
        {
            int value = -distances[index];
            distances[index] = value;
            return value + 1;
        }
    }

    [BurstCompile]
    [ExcludeComponent(typeof(PathElement))]
    [RequireComponentTag(typeof(SmashRockIntention))]
    struct PathToRockJob : IJobForEachWithEntity<Translation>
    {
        public int Width;
        public int Height;
        public int Range;

        [ReadOnly]
        public NativeArray<Entity> Rocks;

        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        int GetTileIndex(int tileX, int tileY) =>  tileY * Width + tileX;

        void Consider(int x, int y, int steps, ref PQueue queue, NativeArray<int> distances)
        {
            var neighborTile = new int2(x, y);
            int neighborIndex = GetTileIndex(x, y);

            if (distances[neighborIndex] > 0) return;

            if (-distances[neighborIndex] > steps)
            {
                distances[neighborIndex] = -steps;
                queue.Enqueue(neighborTile, steps);
            }
        }

        public unsafe void Execute(
            Entity entity,
            int index,
            [ReadOnly] ref Translation position)
        {
            Utils.Init(Width, Height, position.Value, out NativeArray<int> distances, out PQueue queue, out int steps);
            var path = Utils.AddPathToEntity(EntityCommandBuffer, index, entity);

            while (queue.Length > 0 && steps < Range)
            {
                var tile = queue.Dequeue();
                var tileIndex = GetTileIndex(tile.x, tile.y);

                path.Add(new PathElement{ Value = tile });

                var rock = Rocks[tileIndex];
                if (rock != Entity.Null)
                {
                    EntityCommandBuffer.AddComponent(index, entity, new TargetEntity{ Value = rock });
                    break;
                }

                steps = Utils.MarkVisitedAndGetNextDistance(distances, tileIndex);

                if (tile.x + 1 < Width - 1) Consider(tile.x + 1, tile.y, steps, ref queue, distances);
                if (tile.x - 1 > 0) Consider(tile.x - 1, tile.y, steps, ref queue, distances);
                if (tile.y + 1 < Height - 1) Consider(tile.x, tile.y + 1, steps, ref queue, distances);
                if (tile.y - 1 > 0) Consider(tile.x, tile.y - 1, steps, ref queue, distances);
            }

            distances.Dispose();
            queue.Dispose();
        }
    }

    struct DeallocateTempMapDataJob : IJob
    {
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> Rocks;

        [DeallocateOnJobCompletion]
        public NativeArray<int> Plants;

        [DeallocateOnJobCompletion]
        public NativeArray<LandStateType> Land;

        [DeallocateOnJobCompletion]
        public NativeArray<Entity> LandEntities;

        public void Execute() { }
    }

    [BurstCompile]
    [ExcludeComponent(typeof(PathElement))]
    [RequireComponentTag(typeof(TillGroundIntention))]
    struct PathToUntilledJob : IJobForEachWithEntity<Translation>
    {
        public int Width;
        public int Height;
        public int Range;

        [ReadOnly]
        public NativeArray<Entity> Rocks;

        [ReadOnly]
        public NativeArray<LandStateType> Land;

        [ReadOnly]
        public NativeArray<Entity> LandEntities;

        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        int GetTileIndex(int tileX, int tileY) =>  tileY * Width + tileX;

        void Consider(int x, int y, int steps, ref PQueue queue, NativeArray<int> distances)
        {
            var neighborTile = new int2(x, y);
            int neighborIndex = GetTileIndex(x, y);

            if (distances[neighborIndex] > 0 || Rocks[neighborIndex] != Entity.Null) return;

            if (-distances[neighborIndex] > steps)
            {
                distances[neighborIndex] = -steps;
                queue.Enqueue(neighborTile, steps);
            }
        }

        public unsafe void Execute(
            Entity entity,
            int index,
            [ReadOnly] ref Translation position)
        {
            var path = Utils.AddPathToEntity(EntityCommandBuffer, index, entity);
            Utils.Init(Width, Height, position.Value, out NativeArray<int> distances, out PQueue queue, out int steps);

            while (queue.Length > 0 && steps < Range)
            {
                var tile = queue.Dequeue();
                path.Add(new PathElement{ Value = tile });

                var tileIndex = GetTileIndex(tile.x, tile.y);
                if (Land[index] == LandStateType.Untilled)
                {
                    EntityCommandBuffer.AddComponent(index, entity, new TargetEntity { Value = LandEntities[index] });
                    break;
                }

                steps = Utils.MarkVisitedAndGetNextDistance(distances, tileIndex);

                if (tile.x + 1 < Width - 1) Consider(tile.x + 1, tile.y, steps, ref queue, distances);
                if (tile.x - 1 > 0) Consider(tile.x - 1, tile.y, steps, ref queue, distances);
                if (tile.y + 1 < Height - 1) Consider(tile.x, tile.y + 1, steps, ref queue, distances);
                if (tile.y - 1 > 0) Consider(tile.x, tile.y - 1, steps, ref queue, distances);
            }

            distances.Dispose();
            queue.Dispose();
        }
    }

    [BurstCompile]
    [ExcludeComponent(typeof(PathElement))]
    [RequireComponentTag(typeof(HasSeeds), typeof(PlantSeedIntention))]
    struct PathToTilledJob : IJobForEachWithEntity<Translation>
    {
        public int Width;
        public int Height;
        public int Range;

        [ReadOnly]
        public NativeArray<Entity> Rocks;

        [ReadOnly]
        public NativeArray<LandStateType> Land;

        [ReadOnly]
        public NativeArray<Entity> LandEntities;

        public EntityCommandBuffer.Concurrent EntityCommandBuffer;

        int GetTileIndex(int tileX, int tileY) =>  tileY * Width + tileX;

        void Consider(int x, int y, int steps, ref PQueue queue, NativeArray<int> distances)
        {
            var neighborTile = new int2(x, y);
            int neighborIndex = GetTileIndex(x, y);

            if (distances[neighborIndex] > 0 || Rocks[neighborIndex] != Entity.Null) return;

            if (-distances[neighborIndex] > steps)
            {
                distances[neighborIndex] = -steps;
                queue.Enqueue(neighborTile, steps);
            }
        }

        public unsafe void Execute(
            Entity entity,
            int index,
            [ReadOnly] ref Translation position)
        {
            var path = Utils.AddPathToEntity(EntityCommandBuffer, index, entity);
            Utils.Init(Width, Height, position.Value, out NativeArray<int> distances, out PQueue queue, out int steps);

            while (queue.Length > 0 && steps < Range)
            {
                var tile = queue.Dequeue();
                path.Add(new PathElement{ Value = tile });

                var tileIndex = GetTileIndex(tile.x, tile.y);
                if (Land[index] == LandStateType.Untilled)
                {
                    EntityCommandBuffer.AddComponent(index, entity, new TargetEntity { Value = LandEntities[index] });
                    break;
                }

                steps = Utils.MarkVisitedAndGetNextDistance(distances, tileIndex);

                if (tile.x + 1 < Width - 1) Consider(tile.x + 1, tile.y, steps, ref queue, distances);
                if (tile.x - 1 > 0) Consider(tile.x - 1, tile.y, steps, ref queue, distances);
                if (tile.y + 1 < Height - 1) Consider(tile.x, tile.y + 1, steps, ref queue, distances);
                if (tile.y - 1 > 0) Consider(tile.x, tile.y - 1, steps, ref queue, distances);
            }

            distances.Dispose();
            queue.Dispose();
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var mapData = GetSingleton<MapData>();
        var tileCount = mapData.Width * mapData.Height;

        var plantCounts = new NativeArray<int>(tileCount, Allocator.TempJob);
        var createPlantDataHandle = new CreatePlantDataJob
        {
            Width = mapData.Width,
            PlantCounts = plantCounts
        }.Schedule(this, inputDeps);

        var rocks = new NativeArray<Entity>(tileCount, Allocator.TempJob);
        var createRockDataHandle = new CreateRockDataJob
        {
            Width = mapData.Width,
            Rocks = rocks
        }.Schedule(this, inputDeps);

        var land = new NativeArray<LandStateType>(tileCount, Allocator.TempJob);
        var landEntities = new NativeArray<Entity>(tileCount, Allocator.TempJob);
        var createLandDataHandle = new CreateLandDataJob
        {
            Width = mapData.Width,
            Land = land,
            LandEntities = landEntities,
        }.Schedule(this, inputDeps);

        var pathToRockHandle = new PathToRockJob
        {
            Width = mapData.Width,
            Height = mapData.Height,
            Range = 25,
            Rocks = rocks,
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, createRockDataHandle);

        var pathToUntilledHandle = new PathToUntilledJob
        {
            Width = mapData.Width,
            Height = mapData.Height,
            Range = 25,
            Rocks = rocks,
            Land = land,
            LandEntities = landEntities,
            EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, JobHandle.CombineDependencies(createPlantDataHandle, createRockDataHandle));

        var combinedCreationHandles = JobHandle.CombineDependencies(
            createPlantDataHandle,
            createRockDataHandle,
            createLandDataHandle);

        var combinedPathingHandles = JobHandle.CombineDependencies(
            pathToRockHandle,
            pathToUntilledHandle);

        // Cleanup
        var deallocateJob = new DeallocateTempMapDataJob
        {
            Rocks = rocks,
            Plants = plantCounts,
            Land = land,
            LandEntities = landEntities,
        }.Schedule(JobHandle.CombineDependencies(combinedCreationHandles, pathToRockHandle));

        return JobHandle.CombineDependencies(combinedCreationHandles, combinedPathingHandles);
    }
}
