using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver;
using Unity.Serialization.Json;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class GraphLoader : IDisposable
{
    private readonly IDriver _driver;

    public GraphLoader(string uri, string user, string password)
    {
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
    }

    public async Task<List<T>> GetNodes<T>(LatLngBounds bounds) where T : GraphNode<T>, new()
    {
        string tableName = GraphNode<T>.GetTableName();

        await using IAsyncSession session = _driver.AsyncSession();
        var results = await session.ExecuteReadAsync(
            async tx => {
                var reader = await tx.RunAsync(
                    $"MATCH (n : {tableName}) " +
                    $"WHERE (n.latitude >= $latMin) AND (n.latitude <= $latMax)" +
                    $"  AND (n.longitude >= $lngMin) AND (n.longitude <= $lngMax)" +
                    $"RETURN n",
                    new
                    {
                        latMin = bounds.LatMin,
                        latMax = bounds.LatMax,
                        lngMin = bounds.LngMin,
                        lngMax = bounds.LngMax
                    }
                );
                var records = await reader.ToListAsync();
                var results = records.Select(x => GraphNode<T>.FromINode(x["n"].As<INode>())).ToList();
                return results;
            });
        return results;
    }

    public async Task<int> GetTotalCount<T>() where T : GraphNode<T>, new()
    {
        string label = typeof(T).Name;
        string query = $"MATCH (n:{label}) RETURN count(n)";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query);
        var record = await result.SingleAsync();
        return record[0].As<int>();
    }

    public async Task<List<RapidTransitLine>> LoadTransitLinesWithPoints()
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
            MATCH (l:RapidTransitLine)-[:HAS_POINT]->(p:GeoPoint)
            RETURN l.name AS name, collect(p { .latitude, .longitude }) AS points
            ";

            var result = await tx.RunAsync(query);
            var records = await result.ToListAsync();

            var lines = new List<RapidTransitLine>();

            foreach (var record in records)
            {
                var name = record["name"].As<string>();
                var points = record["points"].As<List<IDictionary<string, object>>>();
                //points.Sort((a, b) => ((int)a["idx"]).CompareTo((int)b["idx"]));
                var geoPoints = new List<Vector3>();
                foreach (var point in points)
                {
                    float lat = Convert.ToSingle(point["latitude"]);
                    float lon = Convert.ToSingle(point["longitude"]);
                    geoPoints.Add(new Vector3(lat, 0,  lon));
                }

                lines.Add(new RapidTransitLine
                {
                    lineName = name,
                    geoPoints = geoPoints
                });
            }

            return lines;
        });
    }

    /// <summary>
    /// Get the names and line name of rapid transit stations in the database
    /// </summary>
    /// <returns>A list of the FindStationResult, containing a name and line name</returns>
    public async Task<List<FindStationResult>> GetNamesOfRapidTransitStations()
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var query = @"MATCH (s:RapidTransit) RETURN s;";

            var result = await tx.RunAsync(query);
            var records = await result.ToListAsync();

            var stations = new List<FindStationResult>();

            foreach (var record in records)
            {
                INode node = record["s"].As<INode>();
                string stationName = node["name"].As<string>();
                string lineName = node["line_name"].As<string>();

                FindStationResult findStationResult = new()
                {
                    Name = stationName
                };

                if (lineName.Contains("and"))
                {
                    string[] lines = lineName.Split("and");

                    foreach (string line in lines)
                    {
                        findStationResult.Line = line.Trim();
                        stations.Add(findStationResult);
                    }
                }
                else
                {
                    findStationResult.Line = lineName.Trim();
                    stations.Add(findStationResult);
                }
            }


            return stations;
        });
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }
}

public struct LatLngBounds
{
    public float LatMin, LatMax;
    public float LngMin, LngMax;

    public override string ToString()
    {
        return $"Lat min / max: {{ {LatMin} , {LatMax} }} \nLng min / max: {{ {LngMin} , {LngMax} }}";
    }
}

/// <summary>
/// Class that serves as the result of the GetNamesOfRapidTransitStations() in GraphLoader class
/// </summary>
public struct FindStationResult
{
    public string Name;
    public string Line;

    public override string ToString()
    {
        return $"{Name} on line {Line}.";
    }


}
