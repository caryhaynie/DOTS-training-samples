using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SpawnGroup))]
public class SpawnFarmTilesSystem : ComponentSystem
{
    public NativeArray<Entity> LandEntities;

    protected override void OnDestroy()
    {
        LandEntities.Dispose();
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((Entity e, ref FarmTileSpawner farmTileSpawner, ref MapData mapData) =>
        {
            LandEntities = new NativeArray<Entity>(mapData.Width * mapData.Height, Allocator.Persistent);
            int tileIndex = 0;

            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    var entity = EntityManager.Instantiate(farmTileSpawner.Prefab);
                    EntityManager.AddComponentData<LandState>(entity, new LandState { Value = LandStateType.Untilled });
                    EntityManager.SetComponentData<Translation>(entity, new Translation { Value = new float3(x, 0f, y) });
                    LandEntities[tileIndex++] = entity;
                    if (x == 0 && y == 0)
                        EntityManager.AddComponent<NeedsTilling>(entity);
                }
            }
            EntityManager.DestroyEntity(farmTileSpawner.Prefab);
            EntityManager.RemoveComponent<FarmTileSpawner>(e);
        });
    }
}