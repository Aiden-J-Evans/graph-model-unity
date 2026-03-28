using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// Static class that adds a tag to an entity to confirm the skytrain stations have been loaded
/// </summary>
public static class StationSpawnerBootstrap
{
    /// <summary>
    /// Creates a blob entity to be referenced by the passenger spawner system
    /// </summary>
    /// <param name="positions">the positions of the stations</param>
    public static void CreateBlobEntityFromPositions(List<float3> positions, List<FixedString512Bytes> stationNames)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        var builder = new BlobBuilder(Allocator.Temp);
        ref StationPositionBlob root = ref builder.ConstructRoot<StationPositionBlob>();
        var array = builder.Allocate(ref root.Positions, positions.Count);
        var stations = builder.Allocate(ref root.StationNames, stationNames.Count);

        for (int i = 0; i < positions.Count; i++)
        {
            array[i] = positions[i];
            builder.AllocateString(ref stations[i], stationNames[i].ToString());
        }

        var blob = builder.CreateBlobAssetReference<StationPositionBlob>(Allocator.Persistent);

        builder.Dispose();

        var entity = entityManager.CreateEntity();
        entityManager.AddComponentData(entity, new StationPositionsBlobAsset { Blob = blob });
        entityManager.AddComponent<StationDataReadyTag>(entity);
        Debug.Log("Entities Ready");
    }
}

/// <summary>
/// Allows the StationPositionBlob to be used by Entities
/// </summary>
public struct StationPositionsBlobAsset : IComponentData
{
    public BlobAssetReference<StationPositionBlob> Blob;
}

/// <summary>
/// Defines the shape of the StationsPosition blob
/// </summary>
public struct StationPositionBlob
{
    public BlobArray<float3> Positions;
    public BlobArray<BlobString> StationNames;
}

/// <summary>
/// Tag that is used for when the station data is loaded from the Neo4j database
/// </summary>
public struct StationDataReadyTag : IComponentData { }

