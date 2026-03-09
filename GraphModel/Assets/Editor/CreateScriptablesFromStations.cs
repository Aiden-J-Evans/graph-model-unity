using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
public static class CreateScriptablesFromStations
{
    [MenuItem("Tools/Rapid Transit/Create Scriptable Objects For Stations")]
    private async static void CreateStationData()
    {
        Debug.Log("Running station data creation...");

        try
        {
            GraphLoader graphloader = new GraphLoader("bolt://localhost:7687", "neo4j", DBConfigLoader.LoadDecryptedPassword());
            List<FindStationResult> stations = await graphloader.GetNamesOfRapidTransitStations();
            CreateScriptableObjectsForStations(stations);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Takes in a list of FindStationResult and creates a collections of scriptable objects for use data maniputlation
    /// </summary>
    /// <param name="stations"></param>
    public static void CreateScriptableObjectsForStations(List<FindStationResult> stations)
    {
        EnsureFoldersExist();

        foreach (FindStationResult station in stations)
        {
            string assetPath = $"Assets/Resources/StationUseData/{station.Name} {station.Line}.asset";

            StationUseData existingAsset = AssetDatabase.LoadAssetAtPath<StationUseData>(assetPath);
            if (existingAsset != null)
            {
                Debug.LogWarning("Asset already exists at path: " + assetPath);
                return;
            }

            StationUseData data = ScriptableObject.CreateInstance<StationUseData>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }


    /// <summary>
    /// Makes sure the folders actually exist in database
    /// </summary>
    private static void EnsureFoldersExist()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources/StationUseData"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "StationUseData");
        }
    }
}


