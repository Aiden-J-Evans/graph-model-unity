using Unity.Entities;
using UnityEngine;

public partial class UpdateStationPassengerFlowFromTimeFrameSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<UpdateTimeInterval>();
    }

    protected override void OnUpdate()
    {
        int interval = SystemAPI.GetSingleton<UpdateTimeInterval>().currentInterval;
        Entity requestEntity = SystemAPI.GetSingletonEntity<UpdateTimeInterval>();

        foreach (var (flowData, flowProgress, stationTag) in
            SystemAPI.Query<
                RefRW<SkytrainStationPassengerFlowData>,
                RefRW<SkytrainStationPassengerFlowProgress>,
                RefRO<StationTag>>())
        {
            if (!SkytrainStationRegistry.TryGetStation(stationTag.ValueRO.ID, out SkytrainStation station))
            {
                Debug.LogWarning($"No SkytrainStation found in registry for station ID {stationTag.ValueRO.ID}");
                continue;
            }


            int totalBoardings = 0;
            int totalAlightings = 0;

            var stationUseDatas = station.StationUseDatas;

            if (stationUseDatas != null)
            {

                for (int i = 0; i < stationUseDatas.Count; i++)
                {
                    StationUseData useData = stationUseDatas[i];

                    if (useData == null)
                    {
                        continue;
                    }

                    StationUseData.StationUseHourData hourData = useData.GetDataForHour(interval);
                    Debug.Log($"Station: {station.stationName}\nInterval: {interval}\nBoardings: {hourData.Boardings}\nAlightings: {hourData.Alightings}");
                    totalBoardings += hourData.Boardings;
                    totalAlightings += hourData.Alightings;
                }
            }

            flowData.ValueRW.BoardingsThisInterval = totalBoardings;
            flowData.ValueRW.AlightingsThisInterval = totalAlightings;
            flowData.ValueRW.IntervalDurationSeconds = SimulationTimeManager.IntervalLengthInSecondsRealtime;
            flowData.ValueRW.HourIndex = interval;

            flowProgress.ValueRW.BoardingSpawnProgress = 0f;
            flowProgress.ValueRW.BoardingsSpawnedThisInterval = 0;
            flowProgress.ValueRW.AlightingsProcessedThisInterval = 0;
        }

        EntityManager.RemoveComponent<UpdateTimeInterval>(requestEntity);
    }
}

public struct UpdateTimeInterval : IComponentData
{
    public int currentInterval;
}