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
    public int Value;
}

// Goes on an entity with RockHealth.
public struct BeingAttacked : IComponentData
{
    public int NumAttackers;
}

public struct AttackerList : IBufferElementData
{
    public Entity Value;
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
public struct TillGroundIntention : IComponentData { }
public struct PlantSeedIntention : IComponentData { }
public struct HarvestPlantIntention : IComponentData { }
public struct SellPlantIntention : IComponentData { }

public struct NeedGoal : IComponentData { }
public struct NeedPath : IComponentData { }

//public struct HoldingPlant : IComponentData { }

public struct HasSeeds : IComponentData { }


public struct TargetEntity : IComponentData
{
    public Entity Value;
}
