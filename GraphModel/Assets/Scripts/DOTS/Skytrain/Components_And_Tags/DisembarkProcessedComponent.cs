using Unity.Entities;
using Unity.Mathematics;

public struct DisembarkProcessedComponent : IComponentData
{
    public float3 PositionOfStation;
    public float StationSize;
}
