using Unity.Entities;

using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class PlantGrowthSettingsAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float growthScale = 0.1f;
    public float growthSize = 1f;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData<PlantGrowthSettings>(entity, new PlantGrowthSettings { Scale = growthScale, Size = growthSize });
    }
}