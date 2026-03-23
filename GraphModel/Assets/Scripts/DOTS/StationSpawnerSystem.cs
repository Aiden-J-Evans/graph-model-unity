using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Stateful;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Creates an entity representation for each skytrain station using the blob position reference
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class StationSpawnerSystem : SystemBase
{
    private bool spawned = false;

    protected override void OnUpdate()
    {
        if (spawned) return;

        if (!SystemAPI.HasSingleton<StationDataReadyTag>()) return;
        if (!SystemAPI.TryGetSingleton<StationPositionsBlobAsset>(out var stationBlob)) return;
        if (!SystemAPI.TryGetSingleton<StationPrefabEntity>(out var prefabRef)) return;

        var blob = stationBlob.Blob;
        var prefab = prefabRef.Value;
        ref var positions = ref blob.Value.Positions;

        for (int i = 0; i < positions.Length; i++)
        {
            var instance = EntityManager.Instantiate(prefab);
            EntityManager.SetComponentData(instance, new LocalTransform
            {
                Position = positions[i],
                Rotation = quaternion.identity,
                Scale = 1f
            });
            EntityManager.AddComponentData(instance,
                new SkytrainStationPassengerFlowData
                {
                    CurrentPassengersDisembarkedForTimeFrame = 10,
                    ExpectedMaxPassengersForTimeFrame = 10
                });

            EntityManager.AddBuffer<StatefulTriggerEvent>(instance);
        }

        blob.Dispose();

        EntityManager.RemoveComponent<StationDataReadyTag>(SystemAPI.GetSingletonEntity<StationDataReadyTag>());
        
        spawned = true;
    }
}

