using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public static class StationDatabase
{
    public static readonly List<StationData> CanadaLineStations = new();

    public static readonly List<StationData> ExpoLineStations = new();

    public static readonly List<StationData> MillenniumLineStations = new();

    /// <summary>
    /// Initializes the database for stations. 
    /// </summary>
    /// <param name="stations"></param>
    public static void InitializeDatabase(List<RapidTransit> stations)
    {
        foreach (RapidTransit station in stations)
        {
            Debug.Log("Station id: " + station.id);
            StationData data = new(
                station.stationName,
                station.latitude,
                station.longitude
                );

            if (station.lines.Contains("Canada"))
            {
                CanadaLineStations.Add(data);
            }

            if (station.lines.Contains("Expo"))
            {
                ExpoLineStations.Add(data);
            }

            if (station.lines.Contains("Millennium"))
            {
                MillenniumLineStations.Add(data);
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
        switch (lineName)
        {
            case "Canada Line":
                return CanadaLineStations;
            case "Expo Line":
                return ExpoLineStations;
            case "Millennium Line":
                return MillenniumLineStations;
            default:
                return null;
        }
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

