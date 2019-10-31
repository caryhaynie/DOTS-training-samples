using Unity.Entities;
using Unity.Transforms;

public class SpawnFarmerSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        var prefabManager = GetSingleton<PrefabManager>();
        Entities.ForEach((Entity e, ref SpawnFarmer spawnFarmer, ref Translation translation) => {
            for (int i = 0; i < spawnFarmer.FarmerCount; i++)
            {
                var farmer = PostUpdateCommands.Instantiate(prefabManager.FarmerPrefab);
                PostUpdateCommands.SetComponent<Translation>(farmer, new Translation { Value = translation.Value });
            }
            PostUpdateCommands.RemoveComponent<SpawnFarmer>(e);
        });
    }
}