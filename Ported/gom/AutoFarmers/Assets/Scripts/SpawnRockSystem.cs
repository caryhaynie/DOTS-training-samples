using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SpawnGroup))]
public class SpawnRocksSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        int mapWidth = 0, mapHeight = 0;
        Entities.ForEach((Entity e, ref MapData map, ref RockSpawner rockSpawner) => {
            mapWidth = map.Width;
            mapHeight = map.Height;

            var random = new Random();
            random.InitState();
            for (int attempt = 0; attempt < rockSpawner.SpawnAttempts; attempt++)
            {
                int width = random.NextInt(1, 4);
                int height = random.NextInt(1, 4);
                int rockX = random.NextInt(0, map.Width - width - 1);
                int rockY = random.NextInt(0, map.Height - height - 1);

                // TODO :: verify rocks don't overlap

                var entity = PostUpdateCommands.Instantiate(rockSpawner.Prefab);
                PostUpdateCommands.AddComponent<RockDimensions>(entity, new RockDimensions { Value = new int2(width, height) });
                var StartHealth = GetRockStartingHealth(width, height);
                PostUpdateCommands.AddComponent<RockHealth>(entity, new RockHealth { CurrentHealth = StartHealth, MaxHealth = StartHealth });
                //PostUpdateCommands.SetComponent<Translation>(entity, new Translation { Value = new float3(rockX, GetRockStartingDepth(ref random), rockY) });
                PostUpdateCommands.SetComponent<Translation>(entity, new Translation { Value = new float3(rockX, 0.5f, rockY) }); // setting to standard height to make animations easy
                PostUpdateCommands.AddComponent<NonUniformScale>(entity, new NonUniformScale { Value = new float3 (width, 1f, height) });
                PostUpdateCommands.RemoveComponent<Scale>(entity);
            }

            PostUpdateCommands.DestroyEntity(rockSpawner.Prefab);
            PostUpdateCommands.RemoveComponent<RockSpawner>(e);
        });

        // World.GetOrCreateSystem<RockMapSystem>().RockMap = new NativeArray<Entity>(mapWidth * mapHeight, Allocator.Persistent);
    }

    float GetRockStartingDepth(ref Random r) => r.NextFloat(.4f, .8f);

    int GetRockStartingHealth(int width, int height) => (width + 1) * (height + 1) * 15;
}