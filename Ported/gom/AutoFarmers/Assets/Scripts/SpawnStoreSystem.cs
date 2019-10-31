using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SpawnGroup))]
public class SpawnStoreSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity e, ref MapData mapData, ref StoreSpawner storeSpawner) => {
            var random = new Random();
            random.InitState();
            for (int i = 0; i < storeSpawner.StoreCount; i++)
            {
                var storeX = random.NextInt(0, mapData.Width);
                var storeY = random.NextInt(0, mapData.Height);

                var entity = PostUpdateCommands.Instantiate(storeSpawner.Prefab);
                PostUpdateCommands.AddComponent<Store>(entity);
                PostUpdateCommands.SetComponent<Translation>(entity, new Translation { Value = new float3(storeX, 0f, storeY) });
                if (i == 0) // TODO / HACK :: Move initial farmer creation somewhere else.
                {
                    PostUpdateCommands.AddComponent<SpawnFarmer>(entity, new SpawnFarmer { FarmerCount = 1 });
                }
            }
            PostUpdateCommands.DestroyEntity(storeSpawner.Prefab);
            PostUpdateCommands.RemoveComponent<StoreSpawner>(e);
        });
    }
}