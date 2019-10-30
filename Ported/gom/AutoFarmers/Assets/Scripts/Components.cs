using Unity.Entities;
using Unity.Mathematics;

public struct PathElement : IBufferElementData
{
    public int2 Value;
}

public struct PathIndex : IComponentData
{
    public int Value;
}
