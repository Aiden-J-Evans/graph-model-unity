using JetBrains.Annotations;
using System;
using System.Collections;
using Unity.Entities;
using UnityEngine;

public class PassengerDataManager : MonoBehaviour
{
    [SerializeField] private SimulationTimeManager simulationTimeManager;
    [SerializeField] private float collectDataIntervalSimSeconds = 300;
    private float simDeltaTime;

    // cache the query for efficiency
    EntityQuery query;

    private void Awake()
    {
        simDeltaTime = SimulationTimeManager.GetSimDeltaTime();
    }

    private void Start()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Passenger>());

        StartCoroutine(AddDataToDatabase());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        // TODO make sure we write to file here
    }

    private IEnumerator AddDataToDatabase()
    {
        while (true)
        {
            yield return new WaitForSeconds(simDeltaTime * collectDataIntervalSimSeconds);
            int passengerCount = query.CalculateEntityCount();

            // TODO
            // save to some sort of data object then save to json file
        }
    }



}
