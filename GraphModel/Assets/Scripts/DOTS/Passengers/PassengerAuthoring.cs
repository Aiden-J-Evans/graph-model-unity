using Unity.Entities;
using UnityEngine;

public class PassengerAuthoring : MonoBehaviour
{
    public float moveSpeed = 1f;

    class Baker : Baker<PassengerAuthoring>
    {
        public override void Bake(PassengerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<Passenger>(entity);
            AddComponent(entity, new MoveSpeed { Value = authoring.moveSpeed });
        }
    }
}
