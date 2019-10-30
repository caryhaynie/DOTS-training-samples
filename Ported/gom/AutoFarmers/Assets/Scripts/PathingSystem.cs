using Unity.Burst;
using Unity.Collections;
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
            var width = dimensions.Value.x;
            var height = dimensions.Value.y;
            for (var y = 0; y < height; ++y)
            {
                var endIndex = startIndex + width;
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

    [RequireComponentTag(typeof(SmashRockIntention))]
    struct PathToRockJob : IJobForEach<Translation>
    {
        public int Width;
        public int Height;

        [ReadOnly]
        public NativeArray<byte> Rocks;

        public void Execute(
            [ReadOnly] ref Translation position)
        {
            var startTile = new int2(math.floor(position.Value.xz));
            var startTileIndex = startTile.y * Width + startTile.x;
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

        // var combinedCreationHandles = JobHandle.CombineDependencies(
        //     createPlantDataHandle,
        //     createRockDataHandle,
        //     createLandDataHandle);

        var pathHandle = new PathToRockJob
        {
            Width = mapData.Width,
            Height = mapData.Height,
            Rocks = rocks
        }.Schedule(this, createRockDataHandle);

        return inputDeps;
    }
}
