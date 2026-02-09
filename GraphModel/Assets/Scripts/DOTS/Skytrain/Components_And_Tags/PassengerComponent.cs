using Unity.Entities;
using Unity.Collections;

public struct PassengerComponent : IComponentData
{
    public FixedString64Bytes PassengerName;
}
