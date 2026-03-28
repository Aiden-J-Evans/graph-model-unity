using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct SkytrainExitStationSystem : ISystem
{
    private EntityQuery skytrainExitStationQuery;
    private ComponentLookup<DisembarkProcessedComponent> disembarkProcessedLookup;
    private ComponentLookup<PassengersToDisembarkComponent> passengersToDisembarkLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var skytrainExitStationQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<SkytrainProperties>(),
                ComponentType.ReadOnly<LocalToWorld>()
            },
            Any = new ComponentType[] { 
                typeof(DisembarkProcessedComponent),
                typeof(PassengersToDisembarkComponent)
            },
            None = new ComponentType[] {
            }
        };
        skytrainExitStationQuery = state.GetEntityQuery(skytrainExitStationQueryDesc);

        disembarkProcessedLookup = state.GetComponentLookup<DisembarkProcessedComponent>(true); // true if readonly
        passengersToDisembarkLookup = state.GetComponentLookup<PassengersToDisembarkComponent>(true); // true if readonly
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        disembarkProcessedLookup.Update(ref state);
        passengersToDisembarkLookup.Update(ref state);

        new SkytrainExitSkytrainStationJob
        {
            disembarkProcessedList = disembarkProcessedLookup,
            passengersToDisembarkList = passengersToDisembarkLookup,
            ecb = ecb,
        }.Schedule(skytrainExitStationQuery);
    }

    [BurstCompile]
    public partial struct SkytrainExitSkytrainStationJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<DisembarkProcessedComponent> disembarkProcessedList;
        [ReadOnly]
        public ComponentLookup<PassengersToDisembarkComponent> passengersToDisembarkList;
        public EntityCommandBuffer ecb;
        private void Execute(Entity skytrain, in LocalToWorld skytrainLocalToWorld)
        {
            if (disembarkProcessedList.TryGetComponent(skytrain, out DisembarkProcessedComponent disembarkProcessed))
            {
                // confirm that the skytrain is far enough away from the station
                float distanceFromSkytrainToStation = math.distance(skytrainLocalToWorld.Position, disembarkProcessed.PositionOfStation);
                if (distanceFromSkytrainToStation > disembarkProcessed.StationSize)
                {
                    ecb.RemoveComponent<DisembarkProcessedComponent>(skytrain);
                    if (passengersToDisembarkList.HasComponent(skytrain))
                    {
                        ecb.RemoveComponent<PassengersToDisembarkComponent>(skytrain);
                    }

                }
            }
        }
    }
}

