using Unity.Entities;
using Unity.Mathematics;

public struct PassengersToDisembarkComponent : IComponentData
{
    public int NumberPassengersToDisembark;
    public float3 LocationOfStation;
    public float StationSize;
}
