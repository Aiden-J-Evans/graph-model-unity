using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;


/// <summary>
/// Work in progress system for detection passenger entities around the station entities
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class StationDetectionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        RequireForUpdate<PhysicsWorldSingleton>();
        RequireForUpdate<StationTag>();
    }

    protected override void OnUpdate()
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        Dependency = new StationDetectionJob
        {
            PhysicsWorld = physicsWorld,
            ECB = ecb
        }.ScheduleParallel(Dependency);

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }

    /// <summary>
    /// The job for detecting passengers around a station
    /// </summary>
    [BurstCompile]
    public partial struct StationDetectionJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity stationEntity, in LocalTransform stationTransform)
        {
            var input = new PointDistanceInput
            {
                Position = stationTransform.Position,
                MaxDistance = 2f,
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u, // All layers
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            };

            var hits = new NativeList<DistanceHit>(Allocator.Temp);
            if (PhysicsWorld.CalculateDistance(input, ref hits))
            {
                foreach (var hit in hits)
                {
                    if (hit.Entity != stationEntity) // prevent self-tagging
                    {
                        ECB.AddComponent<StationEntered>(chunkIndex, hit.Entity);
                    }
                }
            }

            hits.Dispose();
        }
    }
}

