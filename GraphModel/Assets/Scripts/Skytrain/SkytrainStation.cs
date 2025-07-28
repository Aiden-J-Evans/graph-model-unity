using TMPro;
using UnityEngine;

public class SkytrainStation : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI stationNameAsset;
    [SerializeField] private TextMeshProUGUI passengerCountAsset;

    [Space(5)]
    [Header("Data")]
    public string stationName;
    public float lat;
    public float lon;
    public GameObject gameObjectRepresentation;

    private int passengerCount = 0;

    public void InitializeStation(string name, float lat, float lon, GameObject gameObjectRepresentation)
    {
        this.name = name;
        this.stationName = name;
        this.lat = lat;
        this.lon = lon;
        this.gameObjectRepresentation = gameObjectRepresentation;
        SetNameAsset(name);
        UpdatePassengerCountAsset();
    }

    public void InitializeStation(string name, float lat, float lon, GameObject gameObjectRepresentation, int passengerCount)
    {
        this.name = name;
        this.lat = lat;
        this.lon = lon;
        this.gameObjectRepresentation = gameObjectRepresentation;
        this.passengerCount = passengerCount;
        UpdatePassengerCountAsset();
    }

    public void IncreasePassengers(int count)
    {
        passengerCount += count;
        UpdatePassengerCountAsset();
    }

    public void DecreasePassengers(int count)
    {
        passengerCount -= count;
        passengerCount = Mathf.Max(passengerCount, 0);
        UpdatePassengerCountAsset();
    }

    private void SetNameAsset(string name)
    {
        stationNameAsset.text = name;
    }

    public void UpdatePassengerCountAsset()
    {
        passengerCountAsset.text = $"Passenger count: {passengerCount}";
    }

    public void ChangeTimeFrame(int timeFrameNumber)
    {
        Debug.Log(gameObjectRepresentation.name + " changed to time frame " + timeFrameNumber);
        // change the number of people expected to exit the trains
        // change the number of people expected to enter the trains
        // set the current number of people having exited trains during time frame to 0
        // set the current number of people having entered trains during time frame to 0
    }
}
