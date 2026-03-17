using Unity.Entities;

public struct PassengerPrototype : IComponentData
{
    public Entity passengerEntity;
    public float distanceBetweenEntities;
}
