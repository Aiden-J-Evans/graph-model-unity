using Unity.Entities;

public struct SkytrainStationPassengerFlowData : IComponentData
{
    public int ExpectedMaxPassengersForTimeFrame;
    public int CurrentPassengersDisembarkedForTimeFrame;
}
