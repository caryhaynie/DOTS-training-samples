using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class SpawnRocksSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        var ecb_system = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        Entities.ForEach((Entity e, ref MapData map, ref RockSpawner rockSpawner) => {
            var random = new Random();
            random.InitState();
            for (int attempt = 0; attempt < rockSpawner.SpawnAttempts; attempt++)
            {
                int width = random.NextInt(0, 4);
                int height = random.NextInt(0, 4);
                int rockX = random.NextInt(0, map.Width - width);
                int rockY = random.NextInt(0, map.Height - height);

                var entity = PostUpdateCommands.Instantiate(rockSpawner.Prefab);
                PostUpdateCommands.AddComponent<RockDimensions>(entity, new RockDimensions { Value = new int2(rockX, rockY) });
                PostUpdateCommands.AddComponent<RockHealth>(entity, new RockHealth { Value = GetRockStartingHealth(width, height) });
                PostUpdateCommands.SetComponent<Translation>(entity, new Translation { Value = new float3(rockX, GetRockStartingDepth(ref random), rockY) });
                PostUpdateCommands.AddComponent<NonUniformScale>(entity, new NonUniformScale { Value = new float3 (width, 1f, height) });
                PostUpdateCommands.RemoveComponent<Scale>(entity);
            }

            PostUpdateCommands.DestroyEntity(rockSpawner.Prefab);
            PostUpdateCommands.RemoveComponent<RockSpawner>(e);
        });
    }

    float GetRockStartingDepth(ref Random r) => r.NextFloat(.4f, .8f);

    int GetRockStartingHealth(int width, int height) => (width + 1) * (height + 1) * 15;
}