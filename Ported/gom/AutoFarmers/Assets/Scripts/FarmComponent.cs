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

public struct StoreSpawner : IComponentData
{
    public Entity Prefab;
    public int StoreCount;
}

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class FarmComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public Vector2Int mapSize = new Vector2Int(64, 64);
    public GameObject dronePrefab;
    public GameObject farmPrefab;
    public GameObject farmerPrefab;
    public GameObject rockPrefab;
    public GameObject storePrefab;
    public int rockSpawnAttempts = 64;
    public int storeCount = 16;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var farmTileSpawner = new FarmTileSpawner {
            Prefab = conversionSystem.GetPrimaryEntity(farmPrefab)
        };
        var rockSpawner = new RockSpawner {
            Prefab = conversionSystem.GetPrimaryEntity(rockPrefab),
            SpawnAttempts = rockSpawnAttempts
        };
        var storeSpawner = new StoreSpawner {
            Prefab = conversionSystem.GetPrimaryEntity(storePrefab),
            StoreCount = storeCount
        };
        var mapData = new MapData {
            Width = mapSize.x,
            Height = mapSize.y
        };
        var prefabManager = new PrefabManager {
            DronePrefab = conversionSystem.GetPrimaryEntity(dronePrefab),
            FarmerPrefab = conversionSystem.GetPrimaryEntity(farmerPrefab)
        };

        dstManager.AddComponentData(entity, farmTileSpawner);
        dstManager.AddComponentData(entity, mapData);
        dstManager.AddComponentData(entity, prefabManager);
        dstManager.AddComponentData(entity, rockSpawner);
        dstManager.AddComponentData(entity, storeSpawner);
        dstManager.AddComponent<Farm>(entity);
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(dronePrefab);
        referencedPrefabs.Add(farmPrefab);
        referencedPrefabs.Add(farmerPrefab);
        referencedPrefabs.Add(rockPrefab);
        referencedPrefabs.Add(storePrefab);
    }
}