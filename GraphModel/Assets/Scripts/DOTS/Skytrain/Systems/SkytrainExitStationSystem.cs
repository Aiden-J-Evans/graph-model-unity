using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Stateful;
using UnityEngine;


[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct SkytrainExitStationSystem : ISystem
{
    private EntityQuery skytrainExitStationQuery;
    private ComponentLookup<DisembarkProcessedTag> disembarkProcessedLookup;
    private ComponentLookup<SkytrainProperties> skytrainPropertiesLookup;
    private ComponentLookup<PassengersToDisembarkComponent> passengersToDisembarkLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var skytrainExitStationQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<StatefulTriggerEvent>(),
                ComponentType.ReadOnly<SkytrainStationPassengerFlowData>()
            },
            None = new ComponentType[] {
            }
        };
        skytrainExitStationQuery = state.GetEntityQuery(skytrainExitStationQueryDesc);

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

        new SkytrainExitSkytrainStationJob
        {
            disembarkProcessedList = disembarkProcessedLookup,
            skytrainPropertiesList = skytrainPropertiesLookup,
            passengersToDisembarkList = passengersToDisembarkLookup,
            ecb = ecb,
        }.Schedule(skytrainExitStationQuery);

        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

    }

    [BurstCompile]
    public partial struct SkytrainExitSkytrainStationJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<DisembarkProcessedTag> disembarkProcessedList;
        [ReadOnly]
        public ComponentLookup<SkytrainProperties> skytrainPropertiesList;
        [ReadOnly]
        public ComponentLookup<PassengersToDisembarkComponent> passengersToDisembarkList;
        public EntityCommandBuffer ecb;
        private void Execute(Entity station, in SkytrainStationPassengerFlowData skytrainStationPassengerFlowData, in DynamicBuffer<StatefulTriggerEvent> statefulTriggerEvents)
        {

            // check if there are a number of trigger events
            if (statefulTriggerEvents.Length > 0)
            {
                // for each trigger event
                for (int i = 0; i < statefulTriggerEvents.Length; i++)
                {
                    // make sure the skytrain is exiting
                    if (statefulTriggerEvents[i].State == StatefulEventState.Exit)
                    {
                        Entity skytrain = statefulTriggerEvents[i].GetOtherEntity(station);
                        // confirm that the other object is a skytrain
                        if (skytrainPropertiesList.TryGetComponent(skytrain, out SkytrainProperties skytrainProperties))
                        {
                            // make sure the other object does not have a 'disembark processed' tag (if it does, continue to next event)
                            if (disembarkProcessedList.HasComponent(skytrain))
                            {
                                ecb.RemoveComponent<DisembarkProcessedTag>(skytrain);
                            }
                            if (passengersToDisembarkList.HasComponent(skytrain))
                            {
                                ecb.RemoveComponent<PassengersToDisembarkComponent>(skytrain);
                            }

                        }
                    }
                    

                }
            }
        }
    }
}

