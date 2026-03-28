using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct SkytrainPassengerDisembarkSystem : ISystem
{
    private EntityQuery skytrainPassengerDisembarkQuery;
    private ComponentLookup<LocalTransform> localTransformLookup;


    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var skytrainPassengerDisembarkQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<LoadingZoneReferenceOnSkytrain>(),
                ComponentType.ReadOnly<PassengersToDisembarkComponent>(),
                ComponentType.ReadOnly<PassengerPrototype>(),
                ComponentType.ReadWrite<SkytrainProperties>(),
                ComponentType.ReadOnly<AllowDisembarkingTag>(),
            },
            None = new ComponentType[] {
                typeof(DisembarkProcessedComponent)
            }
        };
        skytrainPassengerDisembarkQuery = state.GetEntityQuery(skytrainPassengerDisembarkQueryDesc);

        localTransformLookup = state.GetComponentLookup<LocalTransform>(true); // true if readonly

        EntityManager entityManager = state.EntityManager;
        

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        localTransformLookup.Update(ref state);

        new SkytrainPassengerDisembarkJob
        {
            localTransformList = localTransformLookup,
            ecb = ecb,
        }.Schedule(skytrainPassengerDisembarkQuery);

    }

    [BurstCompile]
    public partial struct SkytrainPassengerDisembarkJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<LocalTransform> localTransformList;
        public EntityCommandBuffer ecb;


        private void Execute(Entity skytrain,
            in DynamicBuffer<LoadingZoneReferenceOnSkytrain> loadingZoneReferences,
            in PassengersToDisembarkComponent passengersToDisembark,
            in PassengerPrototype passengerPrototype,
            ref SkytrainProperties skytrainProperties)
        {
            // make sure passengers to disembark is > 0
            if(passengersToDisembark.NumberPassengersToDisembark > 0)
            {
                Debug.Log("DISEMBARKING!!");
                // calculate how many passengers need to be spawned at each loading zone
                float numPassengersToSpawnAtEachLoadingZone = math.ceil((float)passengersToDisembark.NumberPassengersToDisembark / loadingZoneReferences.Length);
                    // numToSpawnAtEachLoadingZone = ceiling(num passengers to spawn / num of loading zones)
                    // NOTE: This should overestimate how many to spawn (the last loading zone will spawn less than this)
                int numberOfPassengersSpawned = 0;
                // Spawn passengers at each loading zone
                for(int l = 0;  l < loadingZoneReferences.Length; l++)
                {
                    // get location of loading zone
                    if (localTransformList.TryGetComponent(loadingZoneReferences[l].LoadingZone, out LocalTransform loadingZoneTransform))
                    {
                        // create a grid around the loading zone based on the size of passengers & Number to spawn
                        // # columns = ceiling(square_root(numToSpawnAtEachLoadingZone))
                        int numColumns = (int)math.ceil(math.sqrt(numPassengersToSpawnAtEachLoadingZone));
                        // # rows = ceiling(numToSpawnAtEachLoadingZone / # columns)
                        int numRows = (int)math.ceil(numPassengersToSpawnAtEachLoadingZone / numColumns);
                        // # horizontal gaps = # columns - 1
                        int numHorizontalGaps = numColumns - 1;
                        // # vertical gaps = # rows - 1
                        int numVerticalGaps = numRows - 1;
                        // x of top left position = location_of_loading_zone_X - (float) (# horizontal gaps / 2)
                        float topLeftX = loadingZoneTransform.Position.x - (float)numHorizontalGaps/2;
                        // z of top left position = location_of_loading_zone_Y - (float) (# vertial gaps / 2)
                        float topLeftZ = loadingZoneTransform.Position.z + (float)numVerticalGaps/2;
                        float topLeftY = loadingZoneTransform.Position.y;
                        int numberOfPassengersSpawnedInLoadingZone = 0;

                        // for # of columns (i)
                        for (int i = 0; i < numColumns; i++)
                        {
                            // for # rows (j)
                            for (int j = 0; j < numRows; j++)
                            {
                                if (numberOfPassengersSpawnedInLoadingZone < numPassengersToSpawnAtEachLoadingZone && numberOfPassengersSpawned < passengersToDisembark.NumberPassengersToDisembark)
                                {
                                    // spawn at (top left + i (gap length) [on x] - j (gap length) [on y]
                                    SpawnPassengerAtLocation(ecb, passengerPrototype.passengerEntity, passengerPrototype.distanceBetweenEntities, topLeftX, topLeftY, topLeftZ, i, j);
                                    // increment # spawned
                                    numberOfPassengersSpawnedInLoadingZone++;
                                    numberOfPassengersSpawned++;
                                }
                                
                            }

                        } 
                    }
                    
                }

                // update the number of passengers on the skytrain (in skytrain properties)
                skytrainProperties.CurrentCapacity = skytrainProperties.CurrentCapacity - numberOfPassengersSpawned;
            }
            else
            {
                //Debug.Log("There were no passengers to disembark from [" + skytrain + "]");
            }

            // add a disembark processed component to skytrain
            ecb.AddComponent<DisembarkProcessedComponent>(skytrain, new DisembarkProcessedComponent
            {
                PositionOfStation = passengersToDisembark.LocationOfStation,
                StationSize = passengersToDisembark.StationSize
            });
            // Remove the passengers to disembark component from skytrain
            ecb.RemoveComponent<PassengersToDisembarkComponent>(skytrain);
            

        }
        private void SpawnPassengerAtLocation(EntityCommandBuffer ecb, Entity passengerPrototype, float gapWidth, float topLeftX, float topLeftY, float topLeftZ, int i, int j)
        {
            
            // calculate positions
            float xPos = topLeftX + i * gapWidth;
            float zPos = topLeftZ - j * gapWidth;
            float3 positionToSpawn = new float3(xPos, topLeftY, zPos);

            // Actually spawn the thing
            Entity passenger = ecb.Instantiate(passengerPrototype);
            ecb.AddComponent<LocalTransform>(passenger, new LocalTransform
            {
                Position = positionToSpawn,
                Scale = 1,
                Rotation = quaternion.identity
            });

            // Passenger needs some sort of "I just got off the skytrain" tag to stop it from being picked up again
            ecb.AddComponent<PassengerGotOffSkytrainTag>(passenger, new PassengerGotOffSkytrainTag { });

            //Debug.Log("Spawning a passenger at " + positionToSpawn);
        }
    }
}
