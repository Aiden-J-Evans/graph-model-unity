using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct SkytrainStartStopSystem : ISystem
{
    private EntityQuery skytrainStartStopQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var skytrainStartStopQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadWrite<SkytrainMotionState>(),
                ComponentType.ReadWrite<SkytrainMotionMagnitude>(),
                ComponentType.ReadOnly<LocalTransform>()
            },
            None = new ComponentType[] {
                //typeof(LoadingZoneInTransitComponent)
            }
        };
        skytrainStartStopQuery = state.GetEntityQuery(skytrainStartStopQueryDesc);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new SkytrainStartStopJob
        {
        }.Schedule(skytrainStartStopQuery);

        // Not sure if need to complete if no EntityCommandBuffer
        //state.Dependency.Complete();

    }

    [BurstCompile]
    public partial struct SkytrainStartStopJob : IJobEntity
    {
        private void Execute(in LocalTransform skytrainTransform, ref SkytrainMotionState skytrainMotionState, ref DynamicBuffer<SkytrainMotionMagnitude> skytrainMotionMagnitudes)
        {
            // calculate current frame movement magnitude
            float3 previousPosition = skytrainMotionState.PreviousPosition;
            float3 movementVector = skytrainTransform.Position - previousPosition;
            float frameMovement = math.length(movementVector);
            // replace 'previous' position with current position
            skytrainMotionState.PreviousPosition = skytrainTransform.Position;
            // get what magnitude spot we are changing
            int magnitudeSpot = skytrainMotionState.CurrentBufferPosition;
            // change that value with current frame movement magnitude
            skytrainMotionMagnitudes[magnitudeSpot] = new SkytrainMotionMagnitude { Value = frameMovement };
            skytrainMotionState.CurrentBufferPosition = (magnitudeSpot+1) % skytrainMotionMagnitudes.Capacity;
            // calculate the total movement
            float totalMovement = 0f;
            for (int i = 0; i < skytrainMotionMagnitudes.Capacity; i++) {
                totalMovement += skytrainMotionMagnitudes[i].Value;
            }

            skytrainMotionState.TotalMovement = totalMovement;

            /*
            // check if that total movement is above threshold, if above it is moving, if below it is stopping
            if(totalMovement > skytrainMotionState.Threshold)
            {
                Debug.Log("ABOVE THRESHOLD " + magnitudeSpot);
            }
            //Debug.Log("Movement: " + totalMovement);
            */
        }
    }
}