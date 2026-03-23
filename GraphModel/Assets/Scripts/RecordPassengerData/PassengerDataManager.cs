using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// Manages the system that collects data on an interval and saves to json file
/// <para>
/// Only works in Editor
/// </para>
/// </summary>
public class PassengerDataManager : MonoBehaviour
{
    [SerializeField] private float collectDataIntervalSimSeconds = 300;
    [SerializeField] private bool autoSave = true;
    [SerializeField] private int autoSaveAfterCollections = 5;
    List<PassengerDataAtTime> collectionData;
    private bool simulationStarted = false;

    // cache the query for efficiency
    EntityQuery waitingQuery;
    EntityQuery inTransitQuery;

    private void Awake()
    {
        collectionData = new();
    }

    private void Start()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // we have to ignore passengers not waiting
        EntityQueryDesc desc = new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Passenger>()
            },
            None = new ComponentType[]
            {
                typeof(PassengerGotOffSkytrainTag)
            }
        };

        waitingQuery = entityManager.CreateEntityQuery(desc);
        inTransitQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SkytrainProperties>());
    }

    private void Update()
    {
        if (!simulationStarted)
        {
            simulationStarted = true;
            Debug.Log("Simulation Started!");
            StartCoroutine(AddDataToDatabase());
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        SaveDataToFile();
    }

    /// <summary>
    /// Adds data periodically to the database
    /// </summary>
    /// <returns></returns>
    private IEnumerator AddDataToDatabase()
    {
        int collections = 1;
        while (true)
        {
            Debug.Log(SimulationTimeManager.ConvertSimSecondsToRealSeconds(collectDataIntervalSimSeconds));
            yield return new WaitForSeconds(SimulationTimeManager.ConvertSimSecondsToRealSeconds(collectDataIntervalSimSeconds));
            // count number of passengers in simulation
            int waitingCount = waitingQuery.CalculateEntityCount();

            NativeArray<SkytrainProperties> skytrains = inTransitQuery.ToComponentDataArray<SkytrainProperties>(Allocator.Temp);

            int inTransitCount = 0;

            foreach (SkytrainProperties skytrain in skytrains)
            {
                inTransitCount += skytrain.CurrentCapacity;
            }

            int currentTime = (int) collectDataIntervalSimSeconds * collections;

            PassengerDataAtTime currentData = new PassengerDataAtTime
            {
                Time = currentTime,
                PassengersWatingAtStations = waitingCount,
                PassengersInTransit = inTransitCount
            };

            collectionData.Add(currentData);


            // autosave
            if (autoSave && collections % autoSaveAfterCollections == 0)
            {
                SaveDataToFile();
            }

            // dispose
            skytrains.Dispose();
        }
    }

    /// <summary>
    /// Saves the timestamp data to a file in the resouces folder
    /// </summary>
    public void SaveDataToFile()
    {
        // create json string
        PassengerDataJSONWrapper wrapper = new PassengerDataJSONWrapper
        {
            Data = collectionData
        };

        string json = JsonUtility.ToJson(wrapper);

        string savePath = Path.Combine(Application.dataPath, "Resources", "PassengerSimulationData.json");

        File.WriteAllText(savePath, json);

        Debug.Log("Passenger Simulation Data saved to Resources");
    }

    /// <summary>
    /// Wrapper for json data
    /// </summary>
    [Serializable]
    public struct PassengerDataJSONWrapper
    {
        public List<PassengerDataAtTime> Data;
    }

    /// <summary>
    /// Data container for passenger timestamps
    /// </summary>
    [Serializable]
    public struct PassengerDataAtTime
    {
        public int Time;
        public int PassengersWatingAtStations;
        public int PassengersInTransit;
    }
}

