using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

/// <summary>
/// Authoring script for adding the station tag to the station entity
/// </summary>
public class StationAuthoring : MonoBehaviour
{
    public float detectionRadius = 2f;

    class Baker : Baker<StationAuthoring>
    {
        public override void Bake(StationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            var sphereGeometry = new SphereGeometry
            {
                Center = float3.zero,
                Radius = authoring.detectionRadius
            };

            var collider = Unity.Physics.SphereCollider.Create(sphereGeometry, CollisionFilter.Default);

            AddComponent(entity, new PhysicsCollider { Value = collider });
            AddComponent<StationTag>(entity);

            // dont dispose
            //collider.Dispose();
        }
    }
}
