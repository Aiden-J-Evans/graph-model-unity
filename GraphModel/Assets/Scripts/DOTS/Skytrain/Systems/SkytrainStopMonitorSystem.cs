using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[RequireMatchingQueriesForUpdate]
[UpdateAfter(typeof(SkytrainStartStopSystem))]
public partial struct SkytrainStopMonitorSystem : ISystem
{
    private EntityQuery skytrainStopQuery;
    private ComponentLookup<Unity.Physics.PhysicsCollider> colliderLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var skytrainStopQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<SkytrainMotionState>(),
                ComponentType.ReadWrite<LoadingZoneReferenceOnSkytrain>(),
            },
            None = new ComponentType[] {
                typeof(SkytrainStoppedTag)
            }
        };
        skytrainStopQuery = state.GetEntityQuery(skytrainStopQueryDesc);

        colliderLookup = state.GetComponentLookup<Unity.Physics.PhysicsCollider>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);


        colliderLookup.Update(ref state);
        new SkytrainStopJob
        {
            colliderLookup = colliderLookup,
            ecb = ecb,
        }.Schedule(skytrainStopQuery);

        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

    }

    [BurstCompile]
    public partial struct SkytrainStopJob : IJobEntity
    {
        public ComponentLookup<Unity.Physics.PhysicsCollider> colliderLookup;
        public EntityCommandBuffer ecb;
        private void Execute(Entity e, in SkytrainMotionState skytrainMotionState, ref DynamicBuffer<LoadingZoneReferenceOnSkytrain> loadingZoneReferences)
        {
            // check if that total movement is above threshold,  if below it is stopping
            if (skytrainMotionState.TotalMovement < skytrainMotionState.Threshold)
            {
                Debug.Log("BELOW THRESHOLD " + skytrainMotionState.CurrentBufferPosition);
                ecb.AddComponent<SkytrainStoppedTag>(e);
                ecb.AddComponent<AllowDisembarkingTag>(e);

                // Enable loading zones
                for (int i = 0; i < loadingZoneReferences.Length; i++)
                {
                    Entity loadingZone = loadingZoneReferences[i].LoadingZone;
                    // show loading zone
                    ecb.RemoveComponent<Unity.Rendering.DisableRendering>(loadingZone);
                    // allow collisions
                    Unity.Physics.CollisionFilter colFilter = Unity.Physics.CollisionFilter.Default;
                    if (colliderLookup.TryGetComponent(loadingZone, out Unity.Physics.PhysicsCollider componentData))
                    {
                        componentData.Value.Value.SetCollisionFilter(colFilter);
                    }

                }
            }
        }
    }
}