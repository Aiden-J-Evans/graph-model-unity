using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using Debug = UnityEngine.Debug;

[BurstCompile]
public partial class MovePassengerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        RequireForUpdate<MoveSpeed>();
        RequireForUpdate<Destination>();
    }

    protected override void OnUpdate()
    {
        GoToDestinationJob goToDestinationJob = new GoToDestinationJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };
        
        goToDestinationJob.ScheduleParallel();
    }


    /// <summary>
    /// Job that carries out the passengers reaching the skytrain
    /// </summary>
    [BurstCompile]
    public partial struct GoToDestinationJob : IJobEntity
    {
        public float deltaTime;
        [BurstCompile]
        public void Execute(ref LocalTransform localTransform, in MoveSpeed moveSpeed, in Destination destination)
        {
            float3 currentPos = localTransform.Position;
            float3 targetPos = destination.Value;

            float3 direction = math.normalize(targetPos - currentPos);
            float3 movement = deltaTime * moveSpeed.Value * direction;

            float distance = math.distance(currentPos, targetPos);
            if (distance < moveSpeed.Value * deltaTime)
            {
                localTransform.Position = targetPos;
                return;
            }

            // Move the entity
            localTransform.Position += movement;
        }
    }
}
