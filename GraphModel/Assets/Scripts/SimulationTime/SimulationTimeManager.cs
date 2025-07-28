using UnityEngine;
using TMPro; // Required for TMP_Text

public class SimulationTimeManager : MonoBehaviour
{
    [Header("UI Text to display the time")]
    public TMP_Text timeText;

    public float timeConversionRealLifeSeconds = 1f;
    public float timeConversionSimulationTimeSeconds = 60f;

    public float timeFrameLengthInSimulationMinutes = 30f;
    public int currentTimeFrameNumber = 0;

    private float timeConversionRealLifeSecondsToSimulationTimeSeconds;
    private float runTime = 0f;
    private float simulationTime = 0f;

    public float nextTimeFrameChange = 30f;

    void Start()
    {
        if (timeText == null)
        {
            Debug.LogWarning("Cannot display Simulation time due to no text object reference set on SimulationtimeManager script");
        }
        timeConversionRealLifeSecondsToSimulationTimeSeconds = timeConversionSimulationTimeSeconds / timeConversionRealLifeSeconds;

        // these values should be standardized
        nextTimeFrameChange = timeFrameLengthInSimulationMinutes;
        currentTimeFrameNumber = 0;
    }

    void Update()
    {
        runTime += Time.deltaTime;
        simulationTime = runTime * timeConversionRealLifeSecondsToSimulationTimeSeconds;

        UpdateTimeDisplay();

        int totalSimMinutes = Mathf.FloorToInt(simulationTime / 60F);
        if (totalSimMinutes >= nextTimeFrameChange)
        {
            currentTimeFrameNumber++; // update time frame
            CallChangeTimeFrameOnStations();
            nextTimeFrameChange += timeFrameLengthInSimulationMinutes; // Schedule next call
        }
    }

    private void UpdateTimeDisplay()
    {
        if (timeText != null)
        {
            int rlMinutes = Mathf.FloorToInt(runTime / 60F);
            int rlSeconds = Mathf.FloorToInt(runTime % 60F);
            int rlMilliseconds = Mathf.FloorToInt((runTime * 1000F) % 1000F);

            int simHours = Mathf.FloorToInt(simulationTime / 3600F);
            int simMinutes = Mathf.FloorToInt((simulationTime % 3600f) / 60F);
            int simSeconds = Mathf.FloorToInt(simulationTime % 60F);
            int simMilliseconds = Mathf.FloorToInt((simulationTime * 1000F) % 1000F);

            int curDailyTimeFrame = currentTimeFrameNumber % (1440 / (int)timeFrameLengthInSimulationMinutes);
            timeText.text = string.Format("Current Time Frame: {0}[{1}]\nReal-Life Time: {2:00}:{3:00}.{4:000}\nSimulation Time: {5:00}:{6:00}:{7:00}.{8:000}",
                curDailyTimeFrame, currentTimeFrameNumber, rlMinutes, rlSeconds, rlMilliseconds, simHours, simMinutes, simSeconds, simMilliseconds);
        }
    }
    private void CallChangeTimeFrameOnStations()
    {
        SkytrainStation[] stations = FindObjectsOfType<SkytrainStation>();

        foreach (var station in stations)
        {
            station.ChangeTimeFrame(currentTimeFrameNumber);
        }
    }
}
