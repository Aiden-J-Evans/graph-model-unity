using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using Unity.VisualScripting.FullSerializer;

public partial class FadeInSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<FadeIn>();
        RequireForUpdate<URPMaterialPropertyBaseColor>();
    }

    protected override void OnUpdate()
    {
        var job = new FadeInJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
        };
        job.ScheduleParallel();
    }
}

/// <summary>
/// Job that uses the passenger shader to fade them in. not super performant
/// </summary>
[BurstCompile]
public partial struct FadeInJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    public void Execute(ref URPMaterialPropertyBaseColor color, ref FadeIn fade)
    {
        fade.Elapsed += DeltaTime;

        float linearT = math.saturate(fade.Elapsed / fade.Duration);
        float t = math.pow(linearT, 3f); // cubic ease-in

        color.Value.w = t;
    }
}