using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StationUseData", menuName = "Scriptable Objects/StationUseData")]
public class StationUseData : ScriptableObject
{
    [SerializeField] private StationUseTimeRangeData[] stationUseTimeRangeDatas;

    public StationUseTimeRangeData[] StationUseTimeRangeDatas => stationUseTimeRangeDatas;

    public enum TimeRange { Morning, Afternoon, Evening, Night }

    private void OnEnable()
    {
        if (stationUseTimeRangeDatas == null || stationUseTimeRangeDatas.Length != 4)
        {
            stationUseTimeRangeDatas = new StationUseTimeRangeData[4];

            stationUseTimeRangeDatas[0].timeRange = TimeRange.Morning;
            stationUseTimeRangeDatas[1].timeRange = TimeRange.Afternoon;
            stationUseTimeRangeDatas[2].timeRange = TimeRange.Evening;
            stationUseTimeRangeDatas[3].timeRange = TimeRange.Night;
        }
    }

    /// <summary>
    /// Describes usage data for a specific time range. Terminal station is whatever end station we choose
    /// </summary>
    [Serializable]
    public struct StationUseTimeRangeData
    {
        public TimeRange timeRange;

        [SerializeField] private int populationHeadedToTerminal;
        public int PopulationHeadedToTerminal => populationHeadedToTerminal;

        [Range(0f, 100f)]
        [SerializeField] private float percentOffToTerminal;
        public float PercentOffToTerminal => percentOffToTerminal;

        [SerializeField] private int populationHeadedAwayTerminal;
        public int PopulationHeadedAwayTerminal => populationHeadedAwayTerminal;

        [Range(0f, 100f)]
        [SerializeField] private float percentOffAwayTerminal;
        public float PercentOffAwayTerminal => percentOffAwayTerminal;
    }

    /// <summary>
    /// Get the data for a specific time period at station
    /// </summary>
    /// <param name="timeRange"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool TryGetDataForPeriod(TimeRange timeRange, out StationUseTimeRangeData data)
    {
        foreach (var rangeData in stationUseTimeRangeDatas)
        {
            if (rangeData.timeRange == timeRange)
            {
                data = rangeData;
                return true;
            }
        }

        data = default;
        return false;
    }
}