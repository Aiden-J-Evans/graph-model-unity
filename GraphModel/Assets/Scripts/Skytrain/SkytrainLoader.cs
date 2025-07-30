using Mono.Cecil;
using Neo4j.Driver;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Color = UnityEngine.Color;

public class SkytrainLoader : MonoBehaviour
{
    

    public static List<SkytrainStation> skytrainStations;
    public List<SkytrainLine> skytrainLines;

    public Material test;

    [Space(5)]
    [Header("Settings")]

    public float stationScale = 10f;
    public bool generateGraph;

    [Space(5)]
    [Header("Prefabs")]
    public GameObject skytrainStationPrefab;
    public GameObject skytrainLinePrefab;
    public GameObject nodeLinePrefab;

    private string database_password;
    private GraphVisualizer graphVisualizer;

    private void Start()
    {
        database_password = DBConfigLoader.LoadDecryptedPassword();
        print(database_password);
        graphVisualizer = FindFirstObjectByType<GraphVisualizer>();
        StartCoroutine(WaitForGraphLoaded());
    }

    /// <summary>
    /// Waits for the first layer of the graph to be loaded, just to prevent any potential errors
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitForGraphLoaded()
    {
        yield return new WaitUntil(() => graphVisualizer.GraphLoaded);

        var loadTask = LoadAndReturnNodes<RapidTransit>();
        while (!loadTask.IsCompleted) yield return null;

        var stations = loadTask.Result;
        StationDatabase.InitializeDatabase(stations);
        InitializeStations(stations);

        TextAsset jsonText = Resources.Load<TextAsset>("RapidTransitGraph");



        if (jsonText != null)
        {
            print("initializing from json");
            string json = jsonText.text;
            InitializeGraphFromJson(json);
        }
        else
        {
            print("initialzing from database");
            var lineTask = LoadTransitLines();
            while (!lineTask.IsCompleted) yield return null;
            InitializeLinesFromData(lineTask.Result);
        }

    }

    /// <summary>
    /// Creates the gameobject representation of the skytrain (rapid transit) stations
    /// </summary>
    /// <param name="stations"></param>
    public void InitializeStations(List<RapidTransit> stations)
    {
        skytrainStations = new();
        for (int i = 0; i < stations.Count; i++)
        {
            RapidTransit originalRepresentation = stations[i];

            GameObject worldRepresentation = Instantiate(skytrainStationPrefab);
            worldRepresentation.name = originalRepresentation.stationName;

            worldRepresentation.transform.position = graphVisualizer.ConvertLatLonToWorld(originalRepresentation.latitude, originalRepresentation.longitude);
            worldRepresentation.transform.localScale = new Vector3(stationScale, stationScale, stationScale);

            worldRepresentation.transform.parent = transform;

            SkytrainStation station = worldRepresentation.GetComponent<SkytrainStation>();
            
            station.InitializeStation(
                originalRepresentation.stationName,
                originalRepresentation.latitude,
                originalRepresentation.longitude,
                worldRepresentation
            );

            skytrainStations.Add(station);

        }
        StationSpawnerBootstrap.CreateBlobEntityFromPositions(skytrainStations.Select(s => (float3) s.transform.position).ToList());

    }

    public void InitializeLinesFromData(List<RapidTransitLine> lines)
    {
        skytrainLines = new();
        foreach (var line in lines)
        {
            var skytrainLine = Instantiate(skytrainLinePrefab);
            skytrainLine.name = line.lineName;
            var skytrainLineScript = skytrainLine.GetComponent<SkytrainLine>();
            var renderer = skytrainLine.GetComponent<LineRenderer>();
            var positionsList = line.geoPoints.Select(p => graphVisualizer.ConvertLatLonToWorld(p.x, p.z)).ToList(); // Assuming lat = x, lon = z 

            var positions = positionsList.ToArray();

            renderer.positionCount = positions.Length;
            renderer.SetPositions(positions);
            renderer.startWidth = renderer.endWidth = 2f;
            renderer.material = new Material(Shader.Find("Sprites/Default")); 
            renderer.startColor = renderer.endColor = SkytrainSystemManager.GetLineColor(line.lineName);

            skytrainLine.transform.parent = transform;

            var stationDatas = StationDatabase.GetStationsFromLine(line.lineName);

            Dictionary<string, SkytrainStation> stations = new();

            foreach (var data in stationDatas)
            {
                var matchedStation = skytrainStations.FirstOrDefault(s => s.stationName == data.Name);
                stations[data.Name] = matchedStation;
            }

            if (generateGraph)
            {
                skytrainLineScript.InitializeLine(
                    line.lineName,
                    SkytrainSystemManager.GetLineColor(line.lineName),
                    positionsList,
                    graphVisualizer,
                    stations
                );
            }
            else
            {
                skytrainLineScript.InitializeLine(
                    line.lineName,
                    SkytrainSystemManager.GetLineColor(line.lineName),
                    positionsList,
                    renderer,
                    graphVisualizer,
                    stations
                );
            }
                

            skytrainLines.Add(skytrainLineScript);

        }
    }

    public void InitializeGraphFromJson(string jsonText)
    {
        SerializableGraph graph = JsonUtility.FromJson<SerializableGraph>(jsonText);

        var idToNodeMap = new Dictionary<string, RapidTransitNode>();

        // create all nodes
        foreach (var line in graph.lines)
        {
            foreach (var serialNode in line.nodes)
            {
                GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.name = serialNode.id;
                var node = nodeObj.AddComponent<RapidTransitNode>();
                node.id = serialNode.id;
                node.lineName = serialNode.lineName;
                node.transform.position = serialNode.position;
                node.transform.position = new Vector3(node.transform.position.x, 0f, node.transform.position.z);
                node.routeIds = serialNode.routeIds;

                idToNodeMap[serialNode.id] = node;

            }
        }

        // connect nodes
        foreach (var line in graph.lines)
        {

            foreach (var serialNode in line.nodes)
            {
                if (!idToNodeMap.TryGetValue(serialNode.id, out var node))
                    continue;

                foreach (var connId in serialNode.connections)
                {
                    if (idToNodeMap.TryGetValue(connId, out var targetNode))
                    {
                        node.connections.Add(targetNode);
                        var display = Instantiate(nodeLinePrefab);
                        display.GetComponent<LineBetweenNodes>().Initialize(
                            node.transform,
                            targetNode.transform,
                            1f,
                            test,
                            line.lineName
                            );
                    }
                }
            }
        }

        SkytrainSystemManager skytrainSystemManager = FindFirstObjectByType<SkytrainSystemManager>();
        skytrainSystemManager.Initialize(graph);
    }

    /// <summary>
    /// Loads the nodes from the Neo4j database and returns them (so we can have gameobject representations)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private async Task<List<T>> LoadAndReturnNodes<T>() where T : GraphNode<T>, new()
    {
        GraphLoader loader = new GraphLoader("bolt://localhost:7687", "neo4j", database_password);
        List<T> results = await loader.GetNodes<T>(graphVisualizer.GetCurrentLatLonBounds());
        int expected = await loader.GetTotalCount<T>();
        Debug.Log($"For type {typeof(T)} Expected: {expected}, got: {results.Count}");
        Debug.Log($"Loaded {results.Count} nodes of type {typeof(T)}");
        return results;
    }

    private async Task<List<RapidTransitLine>> LoadTransitLines()
    {
        GraphLoader loader = new GraphLoader("bolt://localhost:7687", "neo4j", database_password);
        List<RapidTransitLine> results = await loader.LoadTransitLinesWithPoints();
        return results;
    }
}


