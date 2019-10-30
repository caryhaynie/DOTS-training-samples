using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class SpawnFarmTilesSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity e, ref FarmTileSpawner farmTileSpawner, ref MapData mapData) => {
           for (int width = 0; width < mapData.Width; width++)
               for (int height = 0; height < mapData.Height; height++)
               {
                   var entity = PostUpdateCommands.Instantiate(farmTileSpawner.Prefab);
                   PostUpdateCommands.SetComponent<Translation>(entity, new Translation { Value = new float3(width, 0f, height) });
               }
           PostUpdateCommands.DestroyEntity(farmTileSpawner.Prefab);
           PostUpdateCommands.RemoveComponent<FarmTileSpawner>(e);
        });
    }
}