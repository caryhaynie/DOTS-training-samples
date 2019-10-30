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

    [BurstCompile]
    struct CreateRockDataJob : IJobForEach<RockDimensions, Translation>
    {
        public int Width;

        public NativeArray<byte> Rocks;

        public void Execute(
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
                    Rocks[i] = 1;
                }
                startIndex += Width;
            }
        }
    }

    [BurstCompile]
    struct CreateLandDataJob : IJobForEach<Translation, LandState>
    {
        public int Width;
        public NativeArray<LandStateType> Land;

        public void Execute(
            [ReadOnly] ref Translation position,
            [ReadOnly] ref LandState landState)
        {
            var tile = new int2(math.floor(position.Value.xz));
            var tileIndex = tile.y * Width + tile.x;
            Land[tileIndex] = landState.Value;
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

        public int3 Dequeue()
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

            return ret;
        }
    }

    [ExcludeComponent(typeof(PathElement))]
    [RequireComponentTag(typeof(SmashRockIntention))]
    struct PathToRockJob : IJobForEachWithEntity<Translation>
    {
        public int Width;
        public int Height;
        public int Range;

        [ReadOnly]
        public NativeArray<byte> Rocks;

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
            var path = EntityCommandBuffer.AddBuffer<PathElement>(index, entity);

            var distances = new NativeArray<int>(Width * Height, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var minValue = int.MinValue;
            UnsafeUtility.MemCpyReplicate(distances.GetUnsafePtr(), UnsafeUtility.AddressOf(ref minValue), 4, distances.Length);
            // for (int i = 0; i < distances.Length; ++i)
            // {
            //     distances[i] = int.MinValue;
            // }

            int2 currentTile = new int2(math.floor(position.Value.xz));
            int currentIndex = GetTileIndex(currentTile.x, currentTile.y);
            int steps = 0;
            distances[currentIndex] = 0;

            var queue = new PQueue(Width * Height, Allocator.TempJob);
            queue.Enqueue(currentTile, 0);

            while (queue.Length > 0 && steps < Range)
            {
                var currentNode = queue.Dequeue();
                currentTile = currentNode.xy;

                path.Add(new PathElement{Value = currentTile});

                if (Rocks[GetTileIndex(currentTile.x, currentTile.y)] == 1)
                    break;

                steps = currentNode.z + 1;

                if (currentTile.x + 1 < Width - 1) Consider(currentTile.x + 1, currentTile.y, steps, ref queue, distances);
                if (currentTile.x - 1 > 0) Consider(currentTile.x - 1, currentTile.y, steps, ref queue, distances);
                if (currentTile.y + 1 < Height - 1) Consider(currentTile.x, currentTile.y + 1, steps, ref queue, distances);
                if (currentTile.y - 1 > 0) Consider(currentTile.x, currentTile.y - 1, steps, ref queue, distances);
            }

            distances.Dispose();
            queue.Dispose();
        }
    }

    struct DeallocateTempMapDataJob : IJob
    {
        [DeallocateOnJobCompletion]
        public NativeArray<byte> Rocks;

        [DeallocateOnJobCompletion]
        public NativeArray<int> Plants;

        [DeallocateOnJobCompletion]
        public NativeArray<LandStateType> Land;

        public void Execute() { }
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

        var rocks = new NativeArray<byte>(tileCount, Allocator.TempJob);
        var createRockDataHandle = new CreateRockDataJob
        {
            Width = mapData.Width,
            Rocks = rocks
        }.Schedule(this, inputDeps);

        var land = new NativeArray<LandStateType>(tileCount, Allocator.TempJob);
        var createLandDataHandle = new CreateLandDataJob
        {
            Width = mapData.Width,
            Land = land,
        }.Schedule(this, inputDeps);

        var pathToRockHandle = new PathToRockJob
        {
            Width = mapData.Width,
            Height = mapData.Height,
            Range = 25,
            Rocks = rocks
        }.Schedule(this, createRockDataHandle);

        var combinedCreationHandles = JobHandle.CombineDependencies(
            createPlantDataHandle,
            createRockDataHandle,
            createLandDataHandle);

        // Cleanup
        new DeallocateTempMapDataJob
        {
            Rocks = rocks,
            Plants = plantCounts,
            Land = land
        }.Schedule(JobHandle.CombineDependencies(combinedCreationHandles, pathToRockHandle));

        return pathToRockHandle;
    }
}
