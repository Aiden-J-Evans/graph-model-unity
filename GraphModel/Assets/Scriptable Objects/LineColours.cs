using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Scriptable Object class for loading line colours at runtime
/// </summary>
[CreateAssetMenu(fileName = "LineColours", menuName = "Scriptable Objects/LineColours")]
public class LineColours : ScriptableObject
{
    [SerializeField]
    private List<LineData> lineData;

    public Color GetColourFromLine(string lineName)
    {
        return lineData.FirstOrDefault(l => l.lineName == lineName).color;
    }

    public int GetTrainCountFromLine(string lineName)
    {
        return lineData.FirstOrDefault(l => l.lineName == lineName).trains;
    }
}


[Serializable]
public struct LineData
{
    public string lineName;
    public Color color;
    public int trains;
}