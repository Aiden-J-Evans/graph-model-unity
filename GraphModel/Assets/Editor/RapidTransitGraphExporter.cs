using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class RapidTransitGraphExporter : EditorWindow
{
    [MenuItem("Tools/Rapid Transit/Export Graph to JSON")]
    public static void ExportGraph()
    {
        var allNodes = FindObjectsByType<RapidTransitNode>(FindObjectsSortMode.InstanceID);
        var graph = new SerializableGraph();
        var nodeIdMap = new Dictionary<RapidTransitNode, string>();

        Debug.Log($"{allNodes.Length} nodes found");

        // Assign unique IDs
        for (int i = 0; i < allNodes.Length; i++)
        {
            nodeIdMap[allNodes[i]] = $"node_{i}";
        }

        var lines = new Dictionary<string, SerializableLine>();

        foreach (var node in allNodes)
        {
            var serialNode = new SerializableNode
            {
                id = nodeIdMap[node],
                position = node.transform.position,
                lineName = node.lineName,
                connections = new List<string>(),
                routeIds = node.routeIds
            };

            foreach (var connection in node.connections)
            {
                if (connection != null && nodeIdMap.ContainsKey(connection))
                    serialNode.connections.Add(nodeIdMap[connection]);
            }

            if (!lines.TryGetValue(serialNode.lineName, out var line))
            {
                line = new SerializableLine
                {
                    lineName = serialNode.lineName
                };
                lines[serialNode.lineName] = line;
            }
            line.nodes.Add(serialNode);

        }

        // Assign all lines to graph.lines
        graph.lines = lines.Values.ToList();

        // save routes
        foreach (var line in graph.lines)
        {
            // create lists
            Dictionary<int, List<SerializableNode>> lineRouteIds = new();
            foreach (var node in line.nodes)
            {
                foreach (var routeId in node.routeIds)
                {
                    if (!lineRouteIds.TryGetValue(routeId, out var nodeList))
                    {
                        nodeList = new List<SerializableNode>();
                        lineRouteIds[routeId] = nodeList;
                        Debug.Log($"route {routeId} for line {line.lineName} created");
                    }

                    nodeList.Add(node);
                }
            }

            line.routes ??= new List<SerializableRoute>();

            foreach (var route in lineRouteIds)
            {
                line.routes.Add(new SerializableRoute()
                {
                    lineName = line.lineName,
                    routeId = route.Key,
                    nodeIds = route.Value.Select(x => x.id).ToList()
                });
            }


        }

        // Serialize and save
        string json = JsonUtility.ToJson(graph, true);
        string path = EditorUtility.SaveFilePanel("Save Graph JSON", "Assets", "RapidTransitGraph.json", "json");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            Debug.Log($"Graph exported to {path}");
            AssetDatabase.Refresh();
        }
    }

}
