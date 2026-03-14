using Unity.Entities;
using UnityEngine;
using static Unity.Entities.EntitiesJournaling;

public class PassengerMono : MonoBehaviour
{
    public string name;
    public class PassengerBaker : Baker<PassengerMono>
    {
        public override void Bake(PassengerMono authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PassengerComponent { 
                PassengerName = authoring.name
            });
        }
    }
}
