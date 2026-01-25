using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class RapidTransitGraphImporter : EditorWindow
{
    [MenuItem("Tools/Rapid Transit/Import Graph from JSON")]
    public static void ImportGraph()
    {
        string path = EditorUtility.OpenFilePanel("Import Graph JSON", "Assets", "json");
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("No file selected.");
            return;
        }

        string json = File.ReadAllText(path);
        SerializableGraph graph = JsonUtility.FromJson<SerializableGraph>(json);

        if (graph == null || graph.lines == null)
        {
            Debug.LogError("Failed to parse graph.");
            return;
        }

        var idToNodeMap = new Dictionary<string, RapidTransitNode>();

        // First pass: create all nodes
        foreach (var line in graph.lines)
        {
            foreach (var serialNode in line.nodes)
            {
                GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.name = serialNode.id;
                var node = nodeObj.AddComponent<RapidTransitNode>();
                node.id = serialNode.id;
                node.lineName = serialNode.lineName;
                var offset = new Vector3(0f, 5f, 0f);
                node.transform.position = serialNode.position + offset;
                node.routeIds = serialNode.routeIds;

                idToNodeMap[serialNode.id] = node;

                Undo.RegisterCreatedObjectUndo(nodeObj, "Create Node");
            }
        }

        // Second pass: connect nodes
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
                    }
                }
            }
        }

        Debug.Log($"Graph imported. Created {idToNodeMap.Count} nodes.");
    }
}

