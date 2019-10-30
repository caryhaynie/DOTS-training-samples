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

public struct MapData : IComponentData
{
    public int Width;
    public int Height;
}

public struct HarvestablePlant : IComponentData {}

public struct RockDimensions : IComponentData
{
    public int2 Value;
}

public struct RockHealth : IComponentData
{
    public float Value;
}

public enum LandStateType : byte
{
    Tilled,
    Untilled
}

public struct LandState : IComponentData
{
    public LandStateType Value;
}

public struct SmashRockIntention : IComponentData {}
