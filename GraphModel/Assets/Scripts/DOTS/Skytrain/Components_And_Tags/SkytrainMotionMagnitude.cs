using Unity.Entities;

[InternalBufferCapacity(300)]
public struct SkytrainMotionMagnitude : IBufferElementData
{
    public float Value;
}
