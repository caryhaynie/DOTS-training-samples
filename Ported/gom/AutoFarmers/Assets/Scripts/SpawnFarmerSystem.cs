using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(SpawnGroup))]
public class SpawnFarmerSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        var prefabManager = GetSingleton<PrefabManager>();
        Entities.ForEach((Entity e, ref SpawnFarmer spawnFarmer, ref Translation translation) => {
            for (int i = 0; i < spawnFarmer.FarmerCount; i++)
            {
                var farmer = PostUpdateCommands.Instantiate(prefabManager.FarmerPrefab);
                PostUpdateCommands.SetComponent<Translation>(farmer, new Translation { Value = translation.Value - new float3(5, 0, 5) });
            }
            PostUpdateCommands.RemoveComponent<SpawnFarmer>(e);
        });
    }
}