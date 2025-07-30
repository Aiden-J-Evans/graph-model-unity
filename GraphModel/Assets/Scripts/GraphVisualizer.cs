using System.Collections.Generic;
using UnityEngine;
using GD.MinMaxSlider;
using System.Threading.Tasks;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;

public class GraphVisualizer : MonoBehaviour
{
    [SerializeField, MinMaxSlider(0, 1)]
    Vector2 LatitudeBound;

    [SerializeField, MinMaxSlider(0, 1)]
    Vector2 LongitudeBound;

    [SerializeField]
    float ObjectScale = 0.0003f;
    [SerializeField]
    float PositionScale = 100;
    [SerializeField]
    NodeVisuals nodeVisuals;

    [SerializeField]
    Mesh instanceMesh;
    [SerializeField]
    Material instanceMaterial;

    // approx Bounds of Vancouver

    static float graph_lat_min = 49.1670f;
    static float graph_lat_max = 49.2860f;
    static float graph_lng_min = -123.180f;
    static float graph_lng_max = -122.700f;
    Transform graphParent;
    static LatLngBounds b;

    string database_password;

    List<NodeBatch> batches = new();
    public bool GraphLoaded { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        database_password = DBConfigLoader.LoadDecryptedPassword();
        LoadPreferences();
        ReloadGraph();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            SavePreferences();
        }

        if (GraphLoaded)
        {
            RenderBatches();
        }
    }

    private void RenderBatches()
    {
        foreach (var batch in batches)
        {
            int total = batch.matrices.Length;
            const int instanceLimit = 100000;

            MaterialPropertyBlock props = new();
            props.SetVectorArray("_Color", batch.colors);

            for (int i = 0; i < total; i += instanceLimit)
            {
                int count = Mathf.Min(instanceLimit, total - i);
                Graphics.DrawMeshInstanced(batch.mesh, 0, batch.material, batch.matrices, count, props, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0, null, UnityEngine.Rendering.LightProbeUsage.Off);
            }
        }
    }

    private void SavePreferences()
    {
        PlayerPrefs.SetFloat("LatitudeMin", LatitudeBound.x);
        PlayerPrefs.SetFloat("LatitudeMax", LatitudeBound.y);

        PlayerPrefs.SetFloat("LongitudeMin", LongitudeBound.x);
        PlayerPrefs.SetFloat("LongitudeMax", LongitudeBound.y);

        PlayerPrefs.SetFloat("ObjectScale", ObjectScale);
    }

    private void LoadPreferences()
    {
        //float latMin = PlayerPrefs.GetFloat("LatitudeMin", 0.48f);
        //float latMax = PlayerPrefs.GetFloat("LatitudeMax", 0.53f);
        float latMin = PlayerPrefs.GetFloat("LatitudeMin", 0);
        float latMax = PlayerPrefs.GetFloat("LatitudeMax", 1);
        LatitudeBound = new Vector2(latMin, latMax);

        //float lonMin = PlayerPrefs.GetFloat("LongitudeMin", 0.48f);
        //float lonMax = PlayerPrefs.GetFloat("LongitudeMax", 0.53f);
        float lonMin = PlayerPrefs.GetFloat("LongitudeMin", 0);
        float lonMax = PlayerPrefs.GetFloat("LongitudeMax", 1);
        LongitudeBound = new Vector2(lonMin, lonMax);

        UpdateLatLngBounds();

        ObjectScale = PlayerPrefs.GetFloat("ObjectScale", 1f);
    }

    public async void ReloadGraph()
    {
        GraphLoaded = false;
        batches.Clear();
        if (graphParent != null) Destroy(graphParent.gameObject);

        graphParent = new GameObject("Graph").transform;

        UpdateLatLngBounds();

        if (nodeVisuals.junction.enabled) await LoadAndCreateNodeRepresentations<Junction>(nodeVisuals.junction);
        if (nodeVisuals.tree.enabled) await LoadAndCreateNodeRepresentations<Tree>(nodeVisuals.tree);
        if (nodeVisuals.business.enabled) await LoadAndCreateNodeRepresentations<Business>(nodeVisuals.business);
        if (nodeVisuals.store.enabled) await LoadAndCreateNodeRepresentations<Store>(nodeVisuals.store);
        if (nodeVisuals.transit.enabled) await LoadAndCreateNodeRepresentations<Transit>(nodeVisuals.transit);
        if (nodeVisuals.rapidTransit.enabled)
        {
            await LoadAndCreateNodeRepresentations<RapidTransit>(nodeVisuals.rapidTransit);         
        }
        if (nodeVisuals.crime.enabled) await LoadAndCreateNodeRepresentations<Crime>(nodeVisuals.crime);
        if (nodeVisuals.school.enabled) await LoadAndCreateNodeRepresentations<School>(nodeVisuals.school);

        GraphLoaded = true;
    }

    private void UpdateLatLngBounds()
    {
        float graph_lat_range = graph_lat_max - graph_lat_min;
        float graph_lng_range = graph_lng_max - graph_lng_min;
        b.LatMin = graph_lat_min + graph_lat_range * LatitudeBound.x;
        b.LatMax = graph_lat_min + graph_lat_range * LatitudeBound.y;
        b.LngMin = graph_lng_min + graph_lng_range * LongitudeBound.x;
        b.LngMax = graph_lng_min + graph_lng_range * LongitudeBound.y;
    }

    private async Task LoadAndCreateNodeRepresentations<T>(NodeVisual visual) where T : GraphNode<T>, new()
    {
        GraphLoader loader = new GraphLoader("bolt://localhost:7687", "neo4j", database_password);
        List<T> results = await loader.GetNodes<T>(b);
        int expected = await loader.GetTotalCount<T>();
        Debug.Log($"For type {typeof(T)} Expected: {expected}, got: {results.Count}");
        Debug.Log($"Loaded {results.Count} nodes of type {typeof(T)}");

        CreateResultObjects(results, visual);
    }

    private void CreateResultObjects<T>(List<T> nodes, NodeVisual visual) where T : GraphNode<T>, new() 
    {
        List<Matrix4x4> matrices = new();
        List<Vector4> colors = new();

        foreach (T t in nodes)
        {
            Vector3 position = ConvertLatLonToWorld(t.latitude, t.longitude);
            Vector3 scale = Vector3.one * ObjectScale * visual.scale;
            Matrix4x4 trs = Matrix4x4.TRS(position, Quaternion.identity, scale);

            matrices.Add(trs);
            colors.Add((Vector4)visual.color); // includes alpha
        }

        if (matrices.Count > 0)
        {
            NodeBatch batch = new()
            {
                mesh = instanceMesh,
                material = instanceMaterial,
                matrices = matrices.ToArray(),
                colors = colors.ToArray()
            };

            batches.Add(batch);
        }
    }

    public Vector3 ConvertLatLonToWorld(float latitude, float longitude)
    {
        float lat_range = b.LatMax - b.LatMin;
        float lng_range = b.LngMax - b.LngMin;

        return new Vector3(
                longitude - b.LngMin - lng_range / 2,
                0,
                latitude - b.LatMin - lat_range / 2
            ) * PositionScale;
    }

    public static Vector3 ConvertLatLonToWorldStatic(float latitude, float longitude)
    {
        float lat_range = b.LatMax - b.LatMin;
        float lng_range = b.LngMax - b.LngMin;

        return new Vector3(
                longitude - b.LngMin - lng_range / 2,
                0,
                latitude - b.LatMin - lat_range / 2
            ) * 10000;
    }


    public LatLngBounds GetCurrentLatLonBounds()
    {
        return b;
    }
}

[Serializable]
public struct NodeVisuals
{
    public NodeVisual junction;
    public NodeVisual tree;
    public NodeVisual business;
    public NodeVisual store;
    public NodeVisual transit;
    public NodeVisual rapidTransit;
    public NodeVisual crime;
    public NodeVisual school;
}

[Serializable]
public struct NodeVisual
{
    public bool enabled;
    public float scale;
    public float offsetY;
    public Color color;
}

[Serializable]
struct NodeBatch
{
    public Mesh mesh;
    public Material material;
    public Matrix4x4[] matrices;
    public Vector4[] colors; // use Vector4 for color with alpha
}
