using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelDataSO))]
public class LevelDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LevelDataSO levelData = (LevelDataSO)target;
        if (levelData == null) return;

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.25f, 0.75f, 1f, 1f);
        if (GUILayout.Button("Open in Level Editor Window", GUILayout.Height(32f)))
        {
            LevelEditorWindow.OpenWithLevel(levelData);
        }
        GUI.backgroundColor = prevBg;
    }
}
