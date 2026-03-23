using Unity.Entities;
using UnityEngine;

public class SpawnPassengerConfigAuthoring : MonoBehaviour
{
    public GameObject passengerPrefab;
    public int amountToSpawn;

    public class Baker : Baker<SpawnPassengerConfigAuthoring>
    {
        public override void Bake(SpawnPassengerConfigAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new SpawnPassengerConfig
            {
                passengerPrefabEntity = GetEntity(authoring.passengerPrefab, TransformUsageFlags.Dynamic),
                amountToSpawn = authoring.amountToSpawn
            });
        }
    }
}

public struct SpawnPassengerConfig: IComponentData
{
    public Entity passengerPrefabEntity;
    public int amountToSpawn;
}
