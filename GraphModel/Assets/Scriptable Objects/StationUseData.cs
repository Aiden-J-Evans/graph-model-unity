using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StationUseData", menuName = "Scriptable Objects/StationUseData")]
public class StationUseData : ScriptableObject
{
    [SerializeField] private StationUseHourData[] stationUseHourDatas;

    public StationUseHourData[] StationUseHourDatas => stationUseHourDatas;

    private const int StartHour = 5;
    private const int EndHour = 26; // 26 = 2am next day, so last interval is 1-2am

    private void OnEnable()
    {
        int expectedLength = EndHour - StartHour;

        if (stationUseHourDatas == null || stationUseHourDatas.Length != expectedLength)
        {
            stationUseHourDatas = new StationUseHourData[expectedLength];

            for (int i = 0; i < expectedLength; i++)
            {
                stationUseHourDatas[i] = new StationUseHourData
                {
                    startHour = StartHour + i
                };
            }
        }
        else
        {
            // Make sure hours stay aligned even if the asset was resized or reordered
            for (int i = 0; i < stationUseHourDatas.Length; i++)
            {
                stationUseHourDatas[i].startHour = StartHour + i;
            }
        }
    }

    /// <summary>
    /// Describes usage data for a specific hour block
    /// </summary>
    [Serializable]
    public struct StationUseHourData
    {
        [HideInInspector]
        public int startHour;

        [SerializeField] private int alightings;
        public int Alightings => alightings;

        [SerializeField] private int boardings;
        public int Boardings => boardings;

        
    }

    /// <summary>
    /// Get the data for a specific hour at station
    /// </summary>
    /// <param name="interval"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public StationUseHourData GetDataForHour(int interval)
    {
        return stationUseHourDatas[interval];
    }
}