using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;
using Unity.VisualScripting;
using Unity.Serialization.Json;

public class GraphSkytrain : MonoBehaviour
{
    [Header("Skytrain Movement Settings")]
    public float maxSpeedKMPH = 20f;
    public float accelerationKMPH = 2f;
    public float decelerationKMPH = 2.5f;
    public float stopDuration = 3f;

    private int currentPositionIndex;
    private float currentSpeed;
    private SkytrainState currentState;
    private float stopTimer;
    private Vector2 currentPosition;
    private Vector2 nextPosition;
    private float distanceToNext;
    [SerializeField] private List<Vector3> route;

    private HashSet<int> stationIndices = new();
    private Dictionary<int, string> stationIndexToName = new();
    private bool hasTransferredPassengers;

    public GraphSkytrainLine line;


    private int passengerCount = 200;

    private const int MAX_PASSENGERS = 200;

    private int routeDirectionForwards = 1;

    private float unitsPerKm = 100f;
    private float ConvertKmHToUnityUnitsPerSecond(float kmh) => (kmh * unitsPerKm) / 3600f;

    // actual coverted speed values
    private float _maxSpeed;
    private float _acceleration;
    private float _deceleration;


    private enum SkytrainState
    {
        Stopped,
        Accelerating,
        Decelerating,
        Cruising
    }

    private void Start()
    {
        _maxSpeed = ConvertKmHToUnityUnitsPerSecond(maxSpeedKMPH);
        _acceleration = ConvertKmHToUnityUnitsPerSecond(accelerationKMPH);
        _deceleration = ConvertKmHToUnityUnitsPerSecond(decelerationKMPH);
    }


    public void InitializeSkytrain(GraphSkytrainLine line, string name, Vector3 initialPosition, Func<float, float, Vector3> convertLatLonToWorld)
    {
        print("initializing skytrain");
        this.line = line;
        transform.parent = line.gameObject.transform;
        gameObject.name = name;
        transform.position = initialPosition;

        this.route = new List<Vector3>(line.nodes.Select(n => n.position).ToList());
        this.stationIndices = new HashSet<int>();
        this.stationIndexToName = new Dictionary<int, string>();

        var stationList = StationDatabase.GetStationsFromLine(line.lineName);
        var pendingInserts = new List<(int index, string name, Vector3 position)>();

        foreach (var station in stationList)
        {
            Vector3 stationWorldPos = convertLatLonToWorld(station.Latitude, station.Longitude);

            int bestIndex = -1;
            float bestDist = float.MaxValue;

            for (int i = 0; i < route.Count - 1; i++)
            {
                Vector3 a = route[i];
                Vector3 b = route[i + 1];
                Vector3 ab = b - a;
                Vector3 ap = stationWorldPos - a;

                float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
                Vector3 projected = a + t * ab;
                float dist = Vector3.Distance(projected, stationWorldPos);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = i + 1;
                }
            }

            if (bestIndex != -1)
            {
                pendingInserts.Add((bestIndex, station.Name, stationWorldPos));
            }
        }

        pendingInserts.Sort((a, b) => a.index.CompareTo(b.index));

        int offset = 0;
        foreach (var (index, n, pos) in pendingInserts)
        {
            int insertIndex = index + offset;
            route.Insert(insertIndex, pos);
            stationIndices.Add(insertIndex);
            stationIndexToName[insertIndex] = n;
            offset++;
        }
    }

    public float maxDist = 1000f;
    public void InitializeSkytrain(SerializableLine line, int routeId)
    {
        // Step 1: Filter nodes that are part of the route
        var routeNodes = line.nodes
            .Where(n => n.routeIds.Contains(routeId))
            .ToList();

        // Step 2: Map node ID (string) to node object
        var nodeMap = routeNodes.ToDictionary(n => n.id, n => n); // id is string

        // Step 3: Build adjacency list of node ID -> list of connected node IDs
        var adjacency = new Dictionary<string, List<string>>();
        foreach (var node in routeNodes)
        {
            adjacency[node.id] = node.connections
                .Where(connId => nodeMap.ContainsKey(connId)) // only include connections that are also in the route
                .ToList();
        }

        // Step 4: Find a start node (only 1 connection in this route)
        string startId = adjacency.FirstOrDefault(pair => pair.Value.Count == 1).Key;

        if (startId == null)
        {
            Debug.LogError("Could not find a valid start node for route " + routeId);
            return;
        }

        // Step 5: Traverse route with DFS to maintain correct order
        var visited = new HashSet<string>();
        var orderedRoute = new List<Vector3>();

        void DFS(string currentId)
        {
            visited.Add(currentId);
            orderedRoute.Add(nodeMap[currentId].position);

            foreach (var neighbor in adjacency[currentId])
            {
                if (!visited.Contains(neighbor))
                    DFS(neighbor);
            }
        }

        DFS(startId);

        // Step 6: Set route and position
        this.route = orderedRoute;
        transform.position = new Vector3(route[0].x, 0f, route[0].z);
        InitializeStations(line.lineName, maxDist);
    }





    private void Update()
    {
        MoveAlongLine();
    }

    private void InitializeStations(string lineName, float maxStationInsertDistance = 30f)
    {
        this.stationIndices = new HashSet<int>();
        this.stationIndexToName = new Dictionary<int, string>();

        var stationList = StationDatabase.GetStationsFromLine(lineName);
        var pendingInserts = new List<(int index, string name, Vector3 position)>();

        foreach (var station in stationList)
        {
            Vector3 stationWorldPos = GraphVisualizer.ConvertLatLonToWorldStatic(station.Latitude, station.Longitude);
            int bestIndex = -1;
            float bestDist = float.MaxValue;
            Vector3 bestProjected = Vector3.zero;

            for (int i = 0; i < route.Count - 1; i++)
            {
                Vector3 a = route[i];
                Vector3 b = route[i + 1];
                Vector3 ab = b - a;
                Vector3 ap = stationWorldPos - a;

                float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
                Vector3 projected = a + t * ab;

                float dist = Vector3.Distance(projected, stationWorldPos);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = i + 1;
                    bestProjected = projected;
                }
            }

            if (bestIndex != -1 && bestDist <= maxStationInsertDistance)
            {
                pendingInserts.Add((bestIndex, station.Name, bestProjected));
            }
        }

        pendingInserts.Sort((a, b) => a.index.CompareTo(b.index));

        int offset = 0;
        foreach (var (index, n, pos) in pendingInserts)
        {
            int insertIndex = index + offset;
            route.Insert(insertIndex, pos);
            stationIndices.Add(insertIndex);
            stationIndexToName[insertIndex] = n;
            offset++;
        }
    }

    /// <summary>
    /// Handles the movement and interaction between skytrains and the stations.
    /// </summary>
    private void MoveAlongLine()
    {
        if (route == null || route.Count < 2) return;

        Vector3 currentPos = transform.position;
        var over = currentPositionIndex >= route.Count - 1;
        var under = currentPositionIndex <= 0;

        if (over || under)
        {
            TurnAround(over, under);
        }

        Vector3 nextPos = route[currentPositionIndex + routeDirectionForwards];

        Vector3 targetPos = new Vector3(nextPos.x, 0.2f, nextPos.z);
        float distanceToTarget = Vector3.Distance(currentPos, targetPos);

        // Advance to next point if close enough and not a station
        if (distanceToTarget < 0.01f && !stationIndices.Contains(currentPositionIndex + routeDirectionForwards))
        {
            currentPositionIndex += routeDirectionForwards;
            return;
        }

        switch (currentState)
        {
            case SkytrainState.Stopped:

                if (!hasTransferredPassengers && stationIndexToName.TryGetValue(currentPositionIndex + routeDirectionForwards, out string stationName))
                {
                    HandleStationStop(stationName);
                }

                stopTimer -= SimulationTimeManager.GetSimDeltaTime();

                if (stopTimer <= 0f)
                {
                    currentState = SkytrainState.Accelerating;
                    currentPositionIndex += routeDirectionForwards;
                    hasTransferredPassengers = false;
                }
                return;

            case SkytrainState.Accelerating:
                if (DistanceToNextStation() < DecelerationDistance())
                {
                    currentState = SkytrainState.Decelerating;
                    break;
                }

                currentSpeed += _acceleration * SimulationTimeManager.GetSimDeltaTime();
                if (currentSpeed >= _maxSpeed)
                {
                    currentSpeed = _maxSpeed;
                    currentState = SkytrainState.Cruising;
                }
                break;

            case SkytrainState.Cruising:
                if (DistanceToNextStation() < DecelerationDistance())
                {
                    currentState = SkytrainState.Decelerating;
                }
                break;

            case SkytrainState.Decelerating:
                currentSpeed -= _deceleration * SimulationTimeManager.GetSimDeltaTime();

                if (currentSpeed <= 0f)
                {
                    currentSpeed = 0f;
                    transform.position = targetPos;
                    currentState = SkytrainState.Stopped;
                    stopTimer = stopDuration;
                    return;
                }

                break;
        }

        // position
        transform.position = Vector3.MoveTowards(
            currentPos,
            targetPos,
            currentSpeed * SimulationTimeManager.GetSimDeltaTime()
        );

        // rotation
        Vector3 direction = (targetPos - currentPos).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, SimulationTimeManager.GetSimDeltaTime() * 5f);
        }

    }

    /// <summary>
    /// Currently a placeholder for more complex things
    /// </summary>
    private void HandleStationStop(string stationName)
    {
        hasTransferredPassengers = true;
        SkytrainStation station = SkytrainLoader.skytrainStations.Where(s => s.stationName == stationName).FirstOrDefault();


        if (station != null)
        {
            UnloadPassengers(5);
            station.IncreasePassengers(5);
        }

        else
            print("station is null");
    }

    private void TurnAround(bool over, bool under)
    {
        routeDirectionForwards *= -1;

        if (over)
        {
            currentPositionIndex = route.Count - 2;
        }

        if (under)
        {
            currentPositionIndex = 1;
        }
    }


    private float DistanceToNextStation()
    {
        Vector3 currentPos = transform.position;

        if (routeDirectionForwards == 1)
        {
            for (int i = currentPositionIndex + 1; i < route.Count; i++)
            {
                if (stationIndices.Contains(i))
                {
                    Vector3 stationPos = new Vector3(route[i].x, 0f, route[i].z);
                    return Vector3.Distance(currentPos, stationPos);
                }
            }
        }
        else
        {
            for (int i = currentPositionIndex - 1; i > 0; i--)
            {
                if (stationIndices.Contains(i))
                {
                    Vector3 stationPos = new Vector3(route[i].x, 0f, route[i].z);
                    return Vector3.Distance(currentPos, stationPos);
                }
            }
        }


            return float.MaxValue; // No more stations
    }

    public HashSet<int> GetStationIndices(Func<float, float, Vector3> convertLatLonToWorld)
    {
        HashSet<int> stationIndices = new();
        stationIndexToName = new();

        foreach (var station in StationDatabase.GetStationsFromLine(line.lineName))
        {
            
            Vector3 worldPos = convertLatLonToWorld(station.Latitude, station.Longitude);
            int closestIndex = -1;
            float closestDist = float.MaxValue;
            for (int i = 0; i < route.Count; i++)
            {
                float dist = Vector3.Distance(worldPos, route[i]);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }
            if (closestIndex != -1)
            {
                stationIndices.Add(closestIndex);
                stationIndexToName[closestIndex] = station.Name;
            }
        }
        return stationIndices;

    }

    private float DecelerationDistance()
    {
        return (currentSpeed * currentSpeed) / (2 * _deceleration);
    }

    public int LoadPassengers(int count)
    {
        passengerCount += count;

        if (passengerCount > MAX_PASSENGERS)
        {
            var overflow = passengerCount - MAX_PASSENGERS;
            passengerCount = MAX_PASSENGERS;
            return overflow;
        }
        return 0;
    }

    public int UnloadPassengers(int count)
    {
        passengerCount -= count;

        if (passengerCount < 0)
        {
            var overflow = -passengerCount;
            passengerCount = 0;
            return overflow;
        }
        return 0;
    }

    private void OnDrawGizmos()
    {
        if (route == null) return;
        
        for (int idx = 0; idx < route.Count; idx++)
        {
            bool station = stationIndices.Contains(idx);
            Gizmos.color = station ? Color.red : Color.green;
            int size = station ? 3 : 1;
            Gizmos.DrawSphere(route[idx], size);
        }
    }
}
