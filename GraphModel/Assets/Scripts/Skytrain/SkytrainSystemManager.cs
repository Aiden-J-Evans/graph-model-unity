using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class SkytrainSystemManager : MonoBehaviour
{
    public SerializableGraph graph;

    [Space(5)]
    [Header("Prefabs")]
    [SerializeField] private GameObject skytrainPrefab;

    [Space(5)]
    [Header("Debugging")]
    [SerializeField] private bool oneSkytrainOnly = false;

    private float t = 0;

    private float oringialThreshold = 5f;
    private float currrentThreshold;

    private int limit = 10;

    private int spawned = 0;

    private static LineColours lineDataCache;


    private void Awake()
    {
        currrentThreshold = oringialThreshold;
        lineDataCache = Resources.Load<LineColours>("LineColours");
    }

    public void Initialize(SerializableGraph graph)
    {
        this.graph = graph;
        StartCoroutine(CreateSkytrains());
    }

    private void Update()
    {
        if (!oneSkytrainOnly)
        {
            t += Time.deltaTime;

            if (t < 200 && t > currrentThreshold)
            {
                StartCoroutine(CreateSkytrains());
                currrentThreshold += oringialThreshold;
            }
        }
    }


    private IEnumerator CreateSkytrains()
    {
        // defaults currently to canada line
        if (oneSkytrainOnly)
        {
            foreach (var line in graph.lines)
            {
                foreach (var route in line.routes)
                {

                    GameObject skytrain = Instantiate(skytrainPrefab);
                    GraphSkytrain skytrainScript = skytrain.GetComponent<GraphSkytrain>();
                    skytrainScript.InitializeSkytrain(line, route.routeId);
                    spawned++;
                    yield return new WaitForSeconds(1f);
                    break;
                }
                break;
            }

            yield break;
        }

        foreach (var line in graph.lines)
        {
            StartCoroutine(SpawnSkytrainsForLineRoutine(line));
        }
    }

    private IEnumerator SpawnSkytrainsForLineRoutine(SerializableLine line)
    {
        int trainCount = lineDataCache.GetTrainCountFromLine(line.lineName);
        List<int> routeIds = line.routes.Select(e => e.routeId).ToList();
        for (int i = 0; i < trainCount; i++)
        {
            int currentID = routeIds[i % routeIds.Count];
            GameObject skytrain = Instantiate(skytrainPrefab);
            GraphSkytrain skytrainScript = skytrain.GetComponent<GraphSkytrain>();
            skytrainScript.InitializeSkytrain(line, currentID);
            spawned++;
            // wait 4 sim minutes
            yield return new WaitForSeconds(SimulationTimeManager.ConvertSimSecondsToRealSeconds(240f));
        }
    }

   
    /// <summary>
    /// Loads line colour from a ScriptableObject in Resources folder
    /// </summary>
    /// <param name="lineName"></param>
    /// <returns></returns>
    public static Color GetLineColor(string lineName)
    {
        if (lineDataCache == null)
        {
            Debug.LogError("Error loading LineColours from Resources folder");
        }

        return lineDataCache.GetColourFromLine(lineName);
    }


}
