using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class SpawnGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SpawnGroup))]
public class UpdateMapSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(UpdateMapSystemGroup))]
public class SimulateFarmGroup : ComponentSystemGroup { }
