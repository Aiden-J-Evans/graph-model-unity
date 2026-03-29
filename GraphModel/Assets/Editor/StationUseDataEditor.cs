using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StationUseData))]
public class StationUseDataEditor : Editor
{
    private SerializedProperty stationUseHourDatas;

    private void OnEnable()
    {
        stationUseHourDatas = serializedObject.FindProperty("stationUseHourDatas");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hourly Station Usage", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Enter boardings and alightings for each hour block.", MessageType.Info);
        EditorGUILayout.Space();

        if (stationUseHourDatas == null || !stationUseHourDatas.isArray)
        {
            EditorGUILayout.HelpBox("stationUseHourDatas was not found or is not an array.", MessageType.Error);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        for (int i = 0; i < stationUseHourDatas.arraySize; i++)
        {
            SerializedProperty element = stationUseHourDatas.GetArrayElementAtIndex(i);

            SerializedProperty startHourProp = element.FindPropertyRelative("startHour");
            SerializedProperty boardingsProp = element.FindPropertyRelative("boardings");
            SerializedProperty alightingsProp = element.FindPropertyRelative("alightings");

            int startHour = startHourProp.intValue;
            string label = $"{FormatHour(startHour)}-{FormatHour(startHour + 1)}";

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(alightingsProp);
            EditorGUILayout.PropertyField(boardingsProp);
            

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static string FormatHour(int hour)
    {
        int normalizedHour = hour % 24;

        if (normalizedHour == 0)
        {
            return "12am";
        }

        if (normalizedHour < 12)
        {
            return normalizedHour + "am";
        }

        if (normalizedHour == 12)
        {
            return "12pm";
        }

        return (normalizedHour - 12) + "pm";
    }
}