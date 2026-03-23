using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public partial class PassengerWaitTimeSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<Passenger>();
        RequireForUpdate<Destination>();
    }
    protected override void OnUpdate()
    {
        UpdateWaitTimeJob job = new UpdateWaitTimeJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        job.ScheduleParallel();
    }

    public partial struct UpdateWaitTimeJob : IJobEntity
    {
        public float DeltaTime;

        [BurstCompile]
        public void Execute(in LocalTransform transform, ref Passenger passenger, in Destination destination)
        {
            // adjust this value for sensitivity
            if (math.distancesq(transform.Position, destination.Value) <= 1f)
            {
                passenger.TimeWaiting += DeltaTime;
            }
        }
    }
}
