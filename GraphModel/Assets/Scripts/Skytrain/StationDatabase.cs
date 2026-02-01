using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public static class StationDatabase
{
    public static Dictionary<string, List<StationData>> LineStations = new();

    /// <summary>
    /// Initializes the database for stations. 
    /// </summary>
    /// <param name="stations"></param>
    public static void InitializeDatabase(List<RapidTransit> stations)
    {
        foreach (RapidTransit station in stations)
        {
            StationData data = new(
                station.stationName,
                station.latitude,
                station.longitude
            );


            // parse lines
            string stationLines = station.lines;
            var split = stationLines
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);


            HashSet<string> lineSet = new HashSet<string>(split);

            lineSet.Remove("and");
            lineSet.Remove("line");

            foreach (string line in lineSet)
            {
                string key = line + " line";
                if (!LineStations.TryGetValue(key, out var stationDatas))
                {
                    stationDatas = new List<StationData>();
                    LineStations[key] = stationDatas;
                }

                stationDatas.Add(data);
            }
        }
    }

    /// <summary>
    /// Gets all the stations from the given line name
    /// </summary>
    /// <param name="lineName"></param>
    /// <returns></returns>
    public static List<StationData> GetStationsFromLine(string lineName)
    {
        if (LineStations.TryGetValue(lineName.ToLowerInvariant(), out var stationDatas))
        {
            return stationDatas;
        }
        Debug.LogError("No station data associated with specific line name");
        return null;
    }
}

/// <summary>
/// The data representation of stations, containing name, lat, and lon
/// </summary>
public class StationData
{
    public string Name;
    public float Latitude;
    public float Longitude;

    public StationData(string name, float lat, float lon)
    {
        Name = name;
        Latitude = lat;
        Longitude = lon;
    }
}

