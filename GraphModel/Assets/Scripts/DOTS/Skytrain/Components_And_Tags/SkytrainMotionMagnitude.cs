using Unity.Entities;

[InternalBufferCapacity(30)]
public struct SkytrainMotionMagnitude : IBufferElementData
{
    public float Value;
}
