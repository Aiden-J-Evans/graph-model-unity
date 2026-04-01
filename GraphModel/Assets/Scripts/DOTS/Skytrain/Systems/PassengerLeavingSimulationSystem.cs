using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct PassengerLeavingSimulationSystem : ISystem
{
    private EntityQuery passengerLeavingSimulationQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var skytrainExitStationQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<Passenger>(),
                ComponentType.ReadWrite<PassengerExitingSimulationComponent>()
            }
        };
        passengerLeavingSimulationQuery = state.GetEntityQuery(skytrainExitStationQueryDesc);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);


        new PassengerLeavingSimulationJob
        {
            ecb = ecb,
            deltaTime = SystemAPI.Time.DeltaTime,
        }.Schedule(passengerLeavingSimulationQuery);
    }

    [BurstCompile]
    public partial struct PassengerLeavingSimulationJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public float deltaTime;

        private void Execute(Entity passenger, ref PassengerExitingSimulationComponent passengerExitingSimulation)
        {
            passengerExitingSimulation.TimeLeftInSimulation -= deltaTime;
            if (passengerExitingSimulation.TimeLeftInSimulation <0)
            {
                ecb.DestroyEntity(passenger);
            }
        }
    }
}
