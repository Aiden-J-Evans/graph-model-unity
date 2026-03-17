using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Stateful;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct SkytrainEnterStationSystem : ISystem
{
    private EntityQuery skytrainEnterStationQuery;
    private ComponentLookup<DisembarkProcessedTag> disembarkProcessedLookup;
    private ComponentLookup<SkytrainProperties> skytrainPropertiesLookup;
    private ComponentLookup<PassengersToDisembarkComponent> passengersToDisembarkLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var skytrainEnterStationQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<StatefulTriggerEvent>(),
                ComponentType.ReadWrite<SkytrainStationPassengerFlowData>()
            },
            None = new ComponentType[] {
                //typeof(LoadingZoneInTransitComponent) // Doesn't have a 'Disembark Processed' Tag
            }
        };
        skytrainEnterStationQuery = state.GetEntityQuery(skytrainEnterStationQueryDesc);

        disembarkProcessedLookup = state.GetComponentLookup<DisembarkProcessedTag>(true); // true if readonly
        skytrainPropertiesLookup = state.GetComponentLookup<SkytrainProperties>(true); // true if readonly
        passengersToDisembarkLookup = state.GetComponentLookup<PassengersToDisembarkComponent>(true); // true if readonly
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        disembarkProcessedLookup.Update(ref state);
        skytrainPropertiesLookup.Update(ref state);
        passengersToDisembarkLookup.Update(ref state);

        new SkytrainEnterSkytrainStationJob
        {
            disembarkProcessedList = disembarkProcessedLookup,
            skytrainPropertiesList = skytrainPropertiesLookup,
            passengersToDisembarkList = passengersToDisembarkLookup,
            ecb = ecb,
        }.Schedule(skytrainEnterStationQuery);

        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

    }

    [BurstCompile]
    public partial struct SkytrainEnterSkytrainStationJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<DisembarkProcessedTag> disembarkProcessedList;
        [ReadOnly]
        public ComponentLookup<SkytrainProperties> skytrainPropertiesList;
        [ReadOnly]
        public ComponentLookup<PassengersToDisembarkComponent> passengersToDisembarkList;
        public EntityCommandBuffer ecb;
        private void Execute(Entity station, ref SkytrainStationPassengerFlowData skytrainStationPassengerFlowData, in DynamicBuffer<StatefulTriggerEvent> statefulTriggerEvents)
        {
            
            // find out how many passengers you would want to leave this time frame (expected - num left)
            int passengersToDisembarkInTimeFrame = skytrainStationPassengerFlowData.ExpectedMaxPassengersForTimeFrame - skytrainStationPassengerFlowData.CurrentPassengersDisembarkedForTimeFrame;
            // check if there are a number of trigger events and there are passengers left to disembark this time frame
            if (statefulTriggerEvents.Length > 0 && passengersToDisembarkInTimeFrame > 0)
            {
                // create a 'number of disembarked passengers' number, starting at 0
                int passengersDisembarkedInJob = 0;
                // for each trigger event
                for (int i = 0; i < statefulTriggerEvents.Length; i++)
                {
                    if (statefulTriggerEvents[i].State == StatefulEventState.Enter || statefulTriggerEvents[i].State == StatefulEventState.Stay)
                    {
                        Entity skytrain = statefulTriggerEvents[i].GetOtherEntity(station);
                        // confirm that the other object is a skytrain
                        if (skytrainPropertiesList.TryGetComponent(skytrain, out SkytrainProperties skytrainProperties))
                        {
                            // make sure the other object does not have a 'disembark processed' tag (if it does, continue to next event)
                            if (!disembarkProcessedList.HasComponent(skytrain))
                            {
                                // make sure the skytrain doesn't already know to disembark passengers
                                if (!passengersToDisembarkList.HasComponent(skytrain))
                                {
                                    // check how many passengers are on the skytrain, and set the 'to disembark' number either the 'want to disembark' number calculated above or the total num of passengers on the train, whichever is smaller
                                    int numToDisembark = 0;
                                    if (skytrainProperties.CurrentCapacity < passengersToDisembarkInTimeFrame)
                                    {
                                        numToDisembark = skytrainProperties.CurrentCapacity;
                                    }
                                    else
                                    {
                                        numToDisembark = passengersToDisembarkInTimeFrame;
                                    }
                                    // attach a 'passengers to disembark' component to the skytrain with the 'to disembark' value in it
                                    ecb.AddComponent<PassengersToDisembarkComponent>(skytrain, new PassengersToDisembarkComponent { Value = numToDisembark });
                                    // update the 'number of disembarked passengers'
                                    passengersDisembarkedInJob += numToDisembark;
                                    // if 'want to disembark' number is equal to the number of passengers disembarked in job, break the loop
                                    if (passengersToDisembarkInTimeFrame == passengersDisembarkedInJob)
                                    {
                                        break;
                                    }

                                }
                                else
                                {
                                    //Debug.Log("Skytrain [" + skytrain + "] already knows to disembark passengers at this station [" + station + "], so do not ask it to disembark passengers again");
                                }
                            }
                            else
                            {
                                //Debug.Log("Skytrain [" + skytrain + "] already had passengers disembark this time frame at this station [" + station + "], so it cannot disembark passengers again");
                            }

                        }
                    }
                    

                }


                // add the 'number of disembarked passengers' to the CurrentPassengersDisembarkedForTimeFrame in flow data, then save the flow data
                // COMMENT THIS OUT IF YOU DON'T WANT TO LIMIT THE NUMBER OF PASSENGERS TO DISEMBARK EACH TIME FRAME
                //skytrainStationPassengerFlowData.CurrentPassengersDisembarkedForTimeFrame = skytrainStationPassengerFlowData.CurrentPassengersDisembarkedForTimeFrame + passengersDisembarkedInJob;
                
            }
        }
    }
}
