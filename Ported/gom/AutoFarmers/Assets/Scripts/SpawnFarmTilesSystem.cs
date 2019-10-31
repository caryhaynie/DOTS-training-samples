using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SpawnGroup))]
public class SpawnFarmTilesSystem : ComponentSystem
{
    public NativeArray<Entity> LandMap;

    protected override void OnDestroy()
    {
        LandMap.Dispose();
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((Entity e, ref FarmTileSpawner farmTileSpawner, ref MapData mapData) =>
        {
            LandMap = new NativeArray<Entity>(mapData.Width * mapData.Height, Allocator.Persistent);
            int tileIndex = 0;

            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    var entity = PostUpdateCommands.Instantiate(farmTileSpawner.Prefab);
                    PostUpdateCommands.AddComponent<LandState>(entity, new LandState { Value = LandStateType.Untilled });
                    PostUpdateCommands.SetComponent<Translation>(entity, new Translation { Value = new float3(x, 0f, y) });
                    LandMap[tileIndex++] = entity;
                    if (x == 0 && y == 0)
                        PostUpdateCommands.AddComponent<NeedsTilling>(entity);
                }
            }
            PostUpdateCommands.DestroyEntity(farmTileSpawner.Prefab);
            PostUpdateCommands.RemoveComponent<FarmTileSpawner>(e);
        });
    }
}