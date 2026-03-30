using Unity.Entities;

public struct SkytrainStationPassengerFlowData : IComponentData
{
    public int BoardingsThisInterval;
    public int AlightingsThisInterval;
    public float IntervalDurationSeconds;
    public int HourIndex;
}

public struct SkytrainStationPassengerFlowProgress : IComponentData
{
    public float BoardingSpawnProgress;
    public int BoardingsSpawnedThisInterval;
    public int AlightingsProcessedThisInterval;
}
