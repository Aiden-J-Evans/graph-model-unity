using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateAfter(typeof(MovePassengerSystem))]
public partial class CrowdSeparationSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<Radius>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        var query = SystemAPI.QueryBuilder()
            .WithAll<LocalTransform, Radius>()
            .Build();

        var count = query.CalculateEntityCount();
        if (count == 0) return;

        var positions = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var radii = query.ToComponentDataArray<Radius>(Allocator.TempJob);

        var job = new ApplyCrowdSeparationJob
        {
            DeltaTime = deltaTime,
            AllPositions = positions,
            AllRadii = radii
        };

        var handle = job.ScheduleParallel(Dependency);

        // Dispose NativeArrays *after* job runs
        handle = positions.Dispose(handle);
        handle = radii.Dispose(handle);

        Dependency = handle;
    }


    [BurstCompile]
    public partial struct ApplyCrowdSeparationJob : IJobEntity
    {
        public float DeltaTime;

        [ReadOnly] public NativeArray<LocalTransform> AllPositions;
        [ReadOnly] public NativeArray<Radius> AllRadii;

        public void Execute(ref LocalTransform localTransform, in Radius selfRadius)
        {
            float3 currentPos = localTransform.Position;
            float3 separation = float3.zero;
            int neighborCount = 0;

            for (int i = 0; i < AllPositions.Length; i++)
            {
                float3 otherPos = AllPositions[i].Position;
                float otherRadius = AllRadii[i].Value;

                float3 offset = otherPos - currentPos;
                float dist = math.length(offset);
                float combinedRadius = selfRadius.Value + otherRadius;

                if (dist < combinedRadius && dist > 0.0001f)
                {
                    float3 push = -math.normalize(offset) * (combinedRadius - dist);
                    separation += push;
                    neighborCount++;
                }
            }

            if (neighborCount > 0)
            {
                float3 averagePush = separation / neighborCount;
                localTransform.Position += 20f * DeltaTime * averagePush;
            }
        }
    }
}
