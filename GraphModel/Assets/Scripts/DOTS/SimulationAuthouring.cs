using Unity.Entities;
using UnityEngine;

public class SimulationAuthoring : MonoBehaviour
{
    public class Baker : Baker<SimulationAuthoring>
    {
        public override void Bake(SimulationAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent<SimulationTag>(entity);
        }
    }
}