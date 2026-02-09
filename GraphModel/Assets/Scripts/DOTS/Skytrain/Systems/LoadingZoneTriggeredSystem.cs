using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Physics.Stateful;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct LoadingZoneTriggeredSystem : ISystem
{
    private EntityQuery triggeredLoadingZoneQuery;
    private ComponentLookup<SkytrainProperties> skytrainLookup;
    private ComponentLookup<PassengerComponent> passengerLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var triggeredLoadingZoneQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadWrite<LoadingZoneComponent>(),
                ComponentType.ReadWrite<StatefulTriggerEvent>()
            },
            None = new ComponentType[] { 
                typeof(LoadingZoneInTransitComponent) 
            }
        };
        triggeredLoadingZoneQuery = state.GetEntityQuery(triggeredLoadingZoneQueryDesc);

        skytrainLookup = state.GetComponentLookup<SkytrainProperties>(); // true if readonly
        passengerLookup = state.GetComponentLookup<PassengerComponent>(true); // true if readonly
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        skytrainLookup.Update(ref state);
        passengerLookup.Update(ref state);

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);


        new OutputLoazingZoneTriggeredJob { 
            skytrains = skytrainLookup,
            passengers = passengerLookup,
            ecb = ecb
        }.Schedule(triggeredLoadingZoneQuery);

        state.Dependency.Complete();

        // Now that the job is completed, you can enact the changes.
        // Note that Playback can only be called on the main thread.
        ecb.Playback(state.EntityManager);

        // You are responsible for disposing of any ECB you create.
        ecb.Dispose();
    }

    [BurstCompile]
    public partial struct OutputLoazingZoneTriggeredJob : IJobEntity
    {
        public ComponentLookup<SkytrainProperties> skytrains;
        [ReadOnly]
        public ComponentLookup<PassengerComponent> passengers;
        public EntityCommandBuffer ecb;
        private void Execute(ref LoadingZoneComponent loadingZone, ref DynamicBuffer<StatefulTriggerEvent> triggerEvents)
        {
            for (int i = 0; i < triggerEvents.Length; i++)
            {
                string toOutput = "TRIGGERED[" + i + "]";
                StatefulTriggerEvent triggerEvent = triggerEvents[i];
                // first check if what the loading zone collided with is a passenger
                if (passengers.HasComponent(triggerEvent.EntityA) || passengers.HasComponent(triggerEvent.EntityB))
                {
                    toOutput += " and one of the colliders was a passenger";
                    Entity passenger;
                    if (passengers.HasComponent(triggerEvent.EntityA))
                    {
                        passenger = triggerEvent.EntityA;
                        toOutput += " (it was EntityA)";
                    }
                    else
                    {
                        passenger = triggerEvent.EntityB;
                        toOutput += " (it was EntityB)";
                    }

                    // after checking if the loading zone collided with a passenger, check if the skytrain is actually a skytrain
                    if (skytrains.TryGetComponent(loadingZone.SkytrainEntity, out SkytrainProperties targetSkytrainProperties))
                    {
                        if (isSkytrainFull(targetSkytrainProperties))
                        {
                            // Do nothing if skytrain is full
                            // change output to signify the skytrain is full
                            toOutput += " and Skytrain was full";
                        }
                        else
                        {
                            // if the skytrain has space
                            // increase how many people are on the skytrain
                            updateSkytrainDataAsPassengerGetsOn(targetSkytrainProperties, loadingZone.SkytrainEntity);
                            // delete the passenger entity
                            ecb.DestroyEntity(passenger);

                            // change output to signify the skytrain still has room
                            toOutput += " and Skytrain was NOT full!";
                        }
                    }
                }
                else if (skytrains.TryGetComponent(loadingZone.SkytrainEntity, out SkytrainProperties targetSkytrainProperties))
                {
                    isSkytrainFull(targetSkytrainProperties);
                }



                    Debug.Log(toOutput);
            }
        }

        private bool isSkytrainFull(SkytrainProperties targetSkytrainProperties)
        {
            Debug.Log("target skytrain's capacity: [" + targetSkytrainProperties.CurrentCapacity + "/" + targetSkytrainProperties.MaxCapacity + "]");
            
            return targetSkytrainProperties.CurrentCapacity >= targetSkytrainProperties.MaxCapacity;
        }
        private void updateSkytrainDataAsPassengerGetsOn(SkytrainProperties targetSkytrainProperties, Entity targetSkytrain)
        {
            if (targetSkytrainProperties.CurrentCapacity < targetSkytrainProperties.MaxCapacity)
            {
                targetSkytrainProperties.CurrentCapacity++;

                // Save changes
                skytrains[targetSkytrain] = targetSkytrainProperties;
            }
        }
    }

    
    /*
    [BurstCompile]
    public partial struct OutputLoazingZoneTriggeredJob : IJobEntity
    {
        private void Execute(LoadingZoneTriggerAspect loadingZone)
        {
            loadingZone.HandleTriggerEvent();
        }
    }
    */
}
