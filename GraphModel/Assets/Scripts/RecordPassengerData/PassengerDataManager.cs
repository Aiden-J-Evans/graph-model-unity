using System.IO;
using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using UnityEngine.UI;

/// <summary>
/// Manages the system that collects data on an interval and saves to csv file
/// <para>
/// Only works in Editor
/// </para>
/// </summary>
public class PassengerDataManager : MonoBehaviour
{
    [SerializeField] private float collectDataIntervalSimSeconds = 300;
    [SerializeField] private bool autoSave = true;
    [SerializeField] private int autoSaveAfterCollections = 5;
    [SerializeField] private Button saveDataButton;
    private List<PassengerDataAtTime> collectionData;
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

        if (!autoSave)
        {
            saveDataButton.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (!simulationStarted)
        {
            simulationStarted = true;
            StartCoroutine(AddDataToDatabaseSchedule());
        }
    }

    private void OnEnable()
    {
        saveDataButton.onClick.AddListener(SaveDataToFile);
    }

    private void OnDisable()
    {
        saveDataButton.onClick.RemoveListener(SaveDataToFile);

        StopAllCoroutines();

        SaveDataToFile();
    }

    /// <summary>
    /// Adds data periodically to the database
    /// </summary>
    /// <returns></returns>
    private IEnumerator AddDataToDatabaseSchedule()
    {
        int collections = 1;
        while (true)
        {
            yield return new WaitForSeconds(SimulationTimeManager.ConvertSimSecondsToRealSeconds(collectDataIntervalSimSeconds));
            
            PassengerDataAtTime currentData = GetCurrentPassengerData();

            collectionData.Add(currentData);

            // autosave
            if (autoSave && collections % autoSaveAfterCollections == 0)
            {
                SaveDataToFile();
            }          
        }
    }

    private PassengerDataAtTime GetCurrentPassengerData()
    {
        // count number of passengers in simulation
        int waitingCount = waitingQuery.CalculateEntityCount();

        NativeArray<SkytrainProperties> skytrains = inTransitQuery.ToComponentDataArray<SkytrainProperties>(Allocator.Temp);

        int inTransitCount = 0;

        foreach (SkytrainProperties skytrain in skytrains)
        {
            inTransitCount += skytrain.CurrentCapacity;
        }

        PassengerDataAtTime currentData = new PassengerDataAtTime
        {
            Time = (int)SimulationTimeManager.GetCurrentSimTime(),
            PassengersWatingAtStations = waitingCount,
            PassengersInTransit = inTransitCount
        };

        skytrains.Dispose();

        return currentData;
    }

    /// <summary>
    /// Saves the timestamp data to a file in the resouces folder
    /// </summary>
    public void SaveDataToFile()
    {
        StringBuilder sb = new();

        sb.AppendLine("Seconds,Waiting,InTransit");

        foreach (PassengerDataAtTime data in collectionData)
        {
            sb.AppendLine($"{data.Time},{data.PassengersWatingAtStations},{data.PassengersInTransit}");
        }

        string savePath = Path.Combine(Application.dataPath, "Resources", "PassengerSimulationData.csv");

        File.WriteAllText(savePath, sb.ToString());

        Debug.Log("Passenger Simulation Data saved to Resources");
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

