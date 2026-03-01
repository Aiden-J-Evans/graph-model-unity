using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public partial struct SkytrainStartMonitorSystem : ISystem
{
    private EntityQuery skytrainStartQuery;
    private ComponentLookup<AllowDisembarkingTag> allowDisembarkingLookup;
    private ComponentLookup<Unity.Physics.PhysicsCollider> colliderLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var skytrainStartQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<SkytrainMotionState>(),
                ComponentType.ReadOnly<SkytrainStoppedTag>(),
                ComponentType.ReadWrite<LoadingZoneReferenceOnSkytrain>(),
            },
            None = new ComponentType[] {
                //typeof(SkytrainStoppedTag)
            }
        };
        skytrainStartQuery = state.GetEntityQuery(skytrainStartQueryDesc);

        allowDisembarkingLookup = state.GetComponentLookup<AllowDisembarkingTag>(true); // true if readonly
        colliderLookup = state.GetComponentLookup<Unity.Physics.PhysicsCollider>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        allowDisembarkingLookup.Update(ref state);
        colliderLookup.Update(ref state);

        new SkytrainStartJob
        {
            allowDisembarkingList = allowDisembarkingLookup,
            colliderLookup = colliderLookup,
            ecb = ecb,
        }.Schedule(skytrainStartQuery);

        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

    }

    [BurstCompile]
    public partial struct SkytrainStartJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<AllowDisembarkingTag> allowDisembarkingList;
        public ComponentLookup<Unity.Physics.PhysicsCollider> colliderLookup;
        public EntityCommandBuffer ecb;
        private void Execute(Entity e, in SkytrainMotionState skytrainMotionState, ref DynamicBuffer<LoadingZoneReferenceOnSkytrain> loadingZoneReferences)
        {
            // check if that total movement is above threshold,  if above it is starting
            if (skytrainMotionState.TotalMovement >= skytrainMotionState.Threshold)
            {
                Debug.Log("ABOVE THRESHOLD " + skytrainMotionState.CurrentBufferPosition);
                ecb.RemoveComponent<SkytrainStoppedTag>(e);
                if (allowDisembarkingList.HasComponent(e))
                {
                    Debug.Log("Had 'AllowDisembarkingTag', will remove!");
                    ecb.RemoveComponent<AllowDisembarkingTag>(e);
                }

                // Disable loading zones
                for (int i = 0; i < loadingZoneReferences.Length; i++)
                {
                    // stop showing loading zone
                    Entity loadingZone = loadingZoneReferences[i].LoadingZone;
                    ecb.AddComponent<Unity.Rendering.DisableRendering>(loadingZone);
                    // stop allowing collisions
                    Unity.Physics.CollisionFilter colFilter = Unity.Physics.CollisionFilter.Default;
                    colFilter.CollidesWith = 0; // collides with nothing
                    colFilter.BelongsTo = 0; // belongs to no layer
                    if(colliderLookup.TryGetComponent(loadingZone, out Unity.Physics.PhysicsCollider componentData))
                    {
                        componentData.Value.Value.SetCollisionFilter(colFilter);
                    }
                }

            }
        }
    }
}
