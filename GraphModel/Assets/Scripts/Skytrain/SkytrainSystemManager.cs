using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkytrainSystemManager : MonoBehaviour
{
    public SerializableGraph graph;

    [Space(5)]
    [Header("Prefabs")]
    [SerializeField] private GameObject skytrainPrefab;

    private float t = 0;

    private float oringialThreshold = 5f;
    private float currrentThreshold;

    private int limit = 10;

    private int spawned = 0;

    private void Awake()
    {
        currrentThreshold = oringialThreshold;
    }

    public void Initialize(SerializableGraph graph)
    {
        this.graph = graph;
        StartCoroutine(CreateSkytrains());
    }

    private void Update()
    {
        t += Time.deltaTime;

        if (t < 200 && t > currrentThreshold)
        {
            StartCoroutine(CreateSkytrains());
            currrentThreshold += oringialThreshold;
        }
    }


    private IEnumerator CreateSkytrains()
    {
        if (spawned < limit) // TODO: REMOVE THIS CAP LATER
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
                }
            }
        }
    }

    private static readonly Dictionary<string, Color> SkytrainLineColors = new Dictionary<string, Color>
    {
        { "Canada Line", Color.green },
        { "Expo Line", Color.blue },
        { "Millennium Line", new Color(1.0f, 0.84f, 0.0f) }, // gold/yellow
    };

    public static Color GetLineColor(string lineName)
    {
        return SkytrainLineColors[lineName];
    }
}
