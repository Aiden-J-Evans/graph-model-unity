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
    private List<LineData> lineColors;

    public Color GetColourFromLine(string lineName)
    {
        return lineColors.FirstOrDefault(l => l.lineName == lineName).color;
    }
}


[Serializable]
public struct LineData
{
    public string lineName;
    public Color color;
}