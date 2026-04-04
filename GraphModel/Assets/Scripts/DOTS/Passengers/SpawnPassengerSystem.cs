using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering;

public partial class SpawnPassengerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SpawnPassengerConfig>();
        RequireForUpdate<ReadyForPassengerSpawnTag>();
    }

    protected override void OnUpdate()
    {
        var spawnPassengerConfig = SystemAPI.GetSingleton<SpawnPassengerConfig>();
        float deltaTime = SystemAPI.Time.DeltaTime;

        var random = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000f) + 1);

        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        foreach (var (stationTransform, stationResolvedIndex, flowData, flowProgress, stationEntity) in
            SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRO<StationTag>,
                RefRO<SkytrainStationPassengerFlowData>,
                RefRW<SkytrainStationPassengerFlowProgress>>()
            .WithEntityAccess())
        {
            
            float3 stationPosition = stationTransform.ValueRO.Position;
            int stationIndex = stationResolvedIndex.ValueRO.ID;

            if (flowData.ValueRO.IntervalDurationSeconds <= 0f)
            {
                continue;
            }


            if (flowData.ValueRO.BoardingsThisInterval <= 0)
            {
                continue;
            }


            int remainingToSpawn = flowData.ValueRO.BoardingsThisInterval - flowProgress.ValueRO.BoardingsSpawnedThisInterval;
            if (remainingToSpawn <= 0)
            {
                continue;
            }


            float spawnRatePerSecond = (float)flowData.ValueRO.BoardingsThisInterval / flowData.ValueRO.IntervalDurationSeconds;
            flowProgress.ValueRW.BoardingSpawnProgress += spawnRatePerSecond * deltaTime;

            int amountToSpawnThisFrame = (int)math.floor(flowProgress.ValueRO.BoardingSpawnProgress);

            if (amountToSpawnThisFrame <= 0)
            {
                continue;
            }


            amountToSpawnThisFrame = math.min(amountToSpawnThisFrame, remainingToSpawn);
            flowProgress.ValueRW.BoardingSpawnProgress -= amountToSpawnThisFrame;

            for (int i = 0; i < amountToSpawnThisFrame; i++)
            {
                // spawn in radius around staiton
                float angle = random.NextFloat(0f, math.PI * 2f);
                float radius = random.NextFloat(10f, 20f);
                float offsetX = math.cos(angle) * radius;
                float offsetZ = math.sin(angle) * radius;

                float3 randomSpawnPos = stationPosition + new float3(offsetX, 0f, offsetZ);

                Entity spawnedEntity = entityCommandBuffer.Instantiate(spawnPassengerConfig.passengerPrefabEntity);

                entityCommandBuffer.SetComponent(spawnedEntity, new LocalTransform
                {
                    Position = randomSpawnPos,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });

                entityCommandBuffer.AddComponent(spawnedEntity, new Radius
                {
                    Value = 0.5f
                });

                entityCommandBuffer.AddComponent(spawnedEntity, new Destination
                {
                    Value = stationPosition,
                });

                entityCommandBuffer.AddComponent(spawnedEntity, new FadeIn
                {
                    Duration = 15f,
                    Elapsed = 0f
                });

                entityCommandBuffer.AddComponent(spawnedEntity, new URPMaterialPropertyBaseColor
                {
                    Value = new float4(1, 0, 0, 0.0f)
                });

                entityCommandBuffer.AddComponent(spawnedEntity, new Passenger
                {
                    StartStationIndex = stationIndex,
                    TimeWaiting = 0f
                });

            }

            flowProgress.ValueRW.BoardingsSpawnedThisInterval += amountToSpawnThisFrame;
        }

        entityCommandBuffer.Playback(EntityManager);
    }
}