using Unity.Entities;
using Unity.Physics.Stateful;
using UnityEngine;
using static Unity.Entities.EntitiesJournaling;

public class SkytrainStationAuthoring : MonoBehaviour
{
    public int ExpectedNumberOfPassengers;
    public class SkytrainStationBaker : Baker<SkytrainStationAuthoring>
    {
        public override void Bake(SkytrainStationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            
            AddComponent(entity, new SkytrainStationPassengerFlowData
            {
                ExpectedMaxPassengersForTimeFrame = authoring.ExpectedNumberOfPassengers,
                CurrentPassengersDisembarkedForTimeFrame = 0
            });

            AddBuffer<StatefulTriggerEvent>(entity);


        }
    }
}
