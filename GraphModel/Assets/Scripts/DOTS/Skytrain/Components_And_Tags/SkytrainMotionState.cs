using Unity.Entities;
using Unity.Mathematics;

public struct SkytrainMotionState : IComponentData
{
    public float Threshold; // What value
    public float TotalMovement; // Sum of magnitude of movements contained within positions buffer
    public float3 PreviousPosition;
    public int CurrentBufferPosition;
}
