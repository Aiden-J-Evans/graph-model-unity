using Unity.Entities;
using Unity.Collections;

public struct MessageComponent : IComponentData
{
    public FixedString64Bytes Message;
}
