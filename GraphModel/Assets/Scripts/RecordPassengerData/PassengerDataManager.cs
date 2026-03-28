using System.IO;
using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using UnityEngine.UI;
using System.Linq;

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
            Debug.Log("Data Collected");

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
        NativeArray<Passenger> passengers = waitingQuery.ToComponentDataArray<Passenger>(Allocator.Temp);

        NativeArray<SkytrainProperties> skytrains = inTransitQuery.ToComponentDataArray<SkytrainProperties>(Allocator.Temp);

        int inTransitCount = 0;

        foreach (SkytrainProperties skytrain in skytrains)
        {
            inTransitCount += skytrain.CurrentCapacity;
        }

        float averageWait = 0f;

        foreach (Passenger passenger in passengers)
        {
            averageWait += passenger.TimeWaiting;
        }

        averageWait /= waitingCount;

        StationPassengerCountDict stationCounts = new();
        stationCounts.Elements = new();

        foreach (var station in StationDatabase.StationNames)
        {
            Debug.Log($"Station {station}");
            StationPassengerCountDictElement element = new StationPassengerCountDictElement
            {
                StationName = station,
                PassengerCount = passengers.Count(p => p.StartStation == station)
            };
            stationCounts.Elements.Add(element);
        }

        PassengerDataAtTime currentData = new PassengerDataAtTime
        {
            Time = (int)SimulationTimeManager.GetCurrentSimTime(),
            PassengersWatingAtStations = waitingCount,
            PassengersInTransit = inTransitCount,
            AverageWait = averageWait,
            StationPopulations = stationCounts
        };

        skytrains.Dispose();

        return currentData;
    }

    /// <summary>
    /// Saves the timestamp data to a file in the resouces folder
    /// </summary>
    public void SaveDataToFile()
    {
        Debug.Log("Saving to file");
        StringBuilder sb = new();

        string categories = "Seconds,Waiting,InTransit,AverageWait";

        foreach (var station in StationDatabase.StationNames)
        {
            categories += $",{station}";
        }

        sb.AppendLine(categories);

        Debug.Log("Categories added");
        Debug.Log($"Collection data {collectionData == null}");
        foreach (PassengerDataAtTime data in collectionData)
        {
            string main = $"{data.Time},{data.PassengersWatingAtStations},{data.PassengersInTransit},{data.AverageWait}";
            Debug.Log("Data added");
            foreach (var station in StationDatabase.StationNames)
            {
                main += $",{data.StationPopulations.GetCountFromStation(station)}";
                Debug.Log("Aiden " + data.StationPopulations.GetCountFromStation(station));
            }
            sb.AppendLine(main);
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
        public float AverageWait;
        public StationPassengerCountDict StationPopulations;
    }

    [Serializable]
    public struct StationPassengerCountDict
    {
        public List<StationPassengerCountDictElement> Elements;

        public int GetCountFromStation(string station)
        {
            return Elements.FirstOrDefault(e => e.StationName == station).PassengerCount;
        }
    }

    [Serializable]
    public struct StationPassengerCountDictElement
    {
        public string StationName;
        public int PassengerCount;
    }
}

