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
        RequireForUpdate<StationDataReadyTag>();
    }

    protected override void OnUpdate()
    {
        // Run only once
        Enabled = false;

        var spawnPassengerConfig = SystemAPI.GetSingleton<SpawnPassengerConfig>();
        var blobAsset = SystemAPI.GetSingleton<StationPositionsBlobAsset>().Blob;
        ref var positions = ref blobAsset.Value.Positions;
        ref var stations = ref blobAsset.Value.StationNames;

        var random = new Unity.Mathematics.Random((uint)SystemAPI.Time.ElapsedTime + 1);

        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        for (int i = 0; i < spawnPassengerConfig.amountToSpawn; i++)
        {
            // choose semi-random station position
            float3 spawnPos = positions[i % positions.Length];

            // spawn in radius around staiton
            float angle = random.NextFloat(0f, math.PI * 2f);
            float radius = random.NextFloat(10f, 20f);
            float offsetX = math.cos(angle) * radius;
            float offsetZ = math.sin(angle) * radius;

            float3 randomSpawnPos = spawnPos + new float3(offsetX, 0f, offsetZ);

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
                Value = spawnPos,
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
                StartStation = stations[i % stations.Length].ToString(),
                TimeWaiting = 0f
            });
        }

        entityCommandBuffer.Playback(EntityManager);
        Debug.Log("Entities spawned");
    }

    protected override void OnDestroy()
    {
        Entities
            .WithAll<StationPositionsBlobAsset>()
            .ForEach((Entity entity, ref StationPositionsBlobAsset blobAsset) =>
            {
                if (blobAsset.Blob.IsCreated)
                {
                    blobAsset.Blob.Dispose();
                }
            }).WithoutBurst().Run();

        base.OnDestroy();
    }
}


