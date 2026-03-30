using Unity.Entities;
using UnityEngine;

public class SimulationTimeBridge : MonoBehaviour
{
    private EntityManager entityManager;
    private Entity simulationEntity;

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        var query = entityManager.CreateEntityQuery(typeof(SimulationTag));

        if (query.IsEmptyIgnoreFilter)
        {
            Debug.LogError("No SimulationTag entity found.");
            return;
        }

        simulationEntity = query.GetSingletonEntity();
    }

    public void RequestTimeFrameUpdate(int timeFrameNumber)
    {
        if (!entityManager.Exists(simulationEntity))
        {
            Debug.LogError("Simulation entity does not exist.");
            return;
        }

        if (entityManager.HasComponent<UpdateTimeInterval>(simulationEntity))
        {
            entityManager.SetComponentData(simulationEntity, new UpdateTimeInterval
            {
                currentInterval = timeFrameNumber
            });
        }
        else
        {
            entityManager.AddComponentData(simulationEntity, new UpdateTimeInterval
            {
                currentInterval = timeFrameNumber
            });
        }
    }
}