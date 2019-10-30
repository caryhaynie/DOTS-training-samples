using System.Collections.Generic;

using UnityEngine;
using Unity.Entities;
public class FarmInitializer : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {

    }
}