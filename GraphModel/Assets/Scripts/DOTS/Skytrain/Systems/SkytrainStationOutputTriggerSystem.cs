using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Stateful;
using UnityEngine;


[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct SkytrainStationOutputTriggerSystem : ISystem
{
    private EntityQuery skytrainStationOutputTriggerQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var skytrainStationOutputTriggerQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<StatefulTriggerEvent>(),
                ComponentType.ReadOnly<SkytrainStationPassengerFlowData>()
            },
            None = new ComponentType[] {
                //typeof(LoadingZoneInTransitComponent)
            }
        };
        skytrainStationOutputTriggerQuery = state.GetEntityQuery(skytrainStationOutputTriggerQueryDesc);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new SkytrainStationOutputTriggerJob
        {
        }.Schedule(skytrainStationOutputTriggerQuery);

        // Not sure if need to complete if no EntityCommandBuffer
        //state.Dependency.Complete();

    }

    [BurstCompile]
    public partial struct SkytrainStationOutputTriggerJob : IJobEntity
    {
        private void Execute(in SkytrainStationPassengerFlowData skytrainStationPassengerFlowData, in DynamicBuffer<StatefulTriggerEvent> statefulTriggerEvents)
        {
            // check if there are a number of trigger events
            if(statefulTriggerEvents.Length > 0)
            {
                // create an output string
                string toOutput = "";
                // for each trigger event
                for(int i = 0; i < statefulTriggerEvents.Length; i++)
                {
                    if(i > 0)
                    {
                        toOutput += ", ";
                    }
                    // add ID and state value to output
                    toOutput += "[" + i + "] " + statefulTriggerEvents[i].State;
                }


                // output the output string
                Debug.Log("Skytrain Station Triggered: " + toOutput);
            }
        }
    }
}
