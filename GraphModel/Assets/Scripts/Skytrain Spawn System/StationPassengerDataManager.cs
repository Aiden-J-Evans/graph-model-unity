using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StationPassengerDataManager : MonoBehaviour
{
    /// <summary>
    /// Data class for storing station passenger flow data 
    /// </summary>
    [System.Serializable]
    public class StationPassengerData
    {
        public string stationName;
        public List<TimeSlot> timeSlots;

        public override string ToString()
        {
            var representation = $"Station: {stationName} Times:\n";
            
            foreach (var slot in timeSlots)
            {
                representation += slot.ToString() + "\n";
            }

            return representation;
        }
    }

    /// <summary>
    /// Passenger boarding and alighting data for the current time slot
    /// </summary>
    [System.Serializable]
    public class TimeSlot
    {
        public float time;
        public int expectedBoarding;
        public int expectedAlighting;

        public override string ToString()
        {
            return $"Time: {time:F2}, Boarding: {expectedBoarding}, Alighting: {expectedAlighting}";
        }
    }

    /// <summary>
    /// Json wrapper for the station passenger data
    /// </summary>
    [System.Serializable]
    public class StationPassengerDataListWrapper
    {
        public List<StationPassengerData> stations;
    }

    private static List<StationPassengerData> allStations;

    private void Awake()
    {
        LoadStationPassengerFlows();
    }

    private void LoadStationPassengerFlows()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("station_data");
        var wrapper = JsonUtility.FromJson<StationPassengerDataListWrapper>("{\"stations\":" + jsonText.text + "}");
        allStations = wrapper.stations;

        // debug
        foreach (var station in allStations)
        {
            print(station);
        }
    }

    /// <summary>
    /// Gets the expected passenger flow for the current station and time based off of the station_data json file
    /// </summary>
    /// <param name="stationName"></param>
    /// <param name="currentHour"></param>
    /// <returns></returns>
    public static TimeSlot GetExpectedPassengerFlow(string stationName, float currentHour)
    {
        if (allStations == null)
        {
            Debug.LogError("Station passenger flows not loaded!");
            return null;
        }

        var station = allStations.FirstOrDefault(s => s.stationName == stationName);
        if (station == null) return null;

        var exact = station.timeSlots.FirstOrDefault(t => Mathf.Approximately(t.time, currentHour));
        if (exact != null) return exact;

        // fallback
        return station.timeSlots.OrderBy(t => Mathf.Abs(t.time - currentHour)).FirstOrDefault();
    }



}
