using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct SkytrainProperties : IComponentData
{
    public FixedString64Bytes SkytrainName;
    public int MaxCapacity;
    public int CurrentCapacity;
}
