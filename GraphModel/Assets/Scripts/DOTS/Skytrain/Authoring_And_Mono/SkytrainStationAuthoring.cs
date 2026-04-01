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

            AddComponent<SkytrainStationPassengerFlowData>(entity, new SkytrainStationPassengerFlowData
            {
                AlightingsThisInterval = authoring.ExpectedNumberOfPassengers
            });

            AddComponent<SkytrainStationPassengerFlowProgress>(entity);

            AddBuffer<StatefulTriggerEvent>(entity);


        }
    }
}
