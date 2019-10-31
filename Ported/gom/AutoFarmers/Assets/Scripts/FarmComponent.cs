using System.Collections.Generic;

using UnityEngine;
using Unity.Entities;

public struct Farm : IComponentData { }

public struct FarmTileSpawner : IComponentData
{
    public Entity Prefab;
}

public struct RockSpawner : IComponentData
{
    public Entity Prefab;
    public int SpawnAttempts;
}

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class FarmComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public Vector2Int mapSize = new Vector2Int(64, 64);
    public GameObject farmPrefab;
    public GameObject rockPrefab;
    public int rockSpawnAttempts = 64;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var farmTileSpawner = new FarmTileSpawner {
            Prefab = conversionSystem.GetPrimaryEntity(farmPrefab)
        };
        var rockSpawner = new RockSpawner {
            Prefab = conversionSystem.GetPrimaryEntity(rockPrefab),
            SpawnAttempts = rockSpawnAttempts
        };
        var mapData = new MapData {
            Width = mapSize.x,
            Height = mapSize.y
        };

        dstManager.AddComponentData(entity, farmTileSpawner);
        dstManager.AddComponentData(entity, rockSpawner);
        dstManager.AddComponentData(entity, mapData);
        dstManager.AddComponent<Farm>(entity);
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(farmPrefab);
        referencedPrefabs.Add(rockPrefab);
    }
}