using Unity.Entities;
using UnityEngine;

/// <summary>
/// The baker for the station prefab. this allows for the station prefab to be instantiated as a prefab at runtime
/// </summary>
public class StationPrefabBaker : Baker<StationPrefabReference>
{
    public override void Bake(StationPrefabReference authoring)
    {
        Debug.Log("baking");

        // The entity we're baking this component onto (our own GameObject)
        var selfEntity = GetEntity(TransformUsageFlags.None);

        // Grab the baked entity for the prefab
        var prefabEntity = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic);

        // Store a reference to the prefabEntity in our own entity's component
        AddComponent(selfEntity, new StationPrefabEntity { Value = prefabEntity });
    }
}

public struct StationPrefabEntity : IComponentData
{
    public Entity Value;
}
