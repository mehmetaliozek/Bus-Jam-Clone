using UnityEditor;
using UnityEngine;

public static class LevelDataCreator
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string LevelsFolder = ResourcesFolder + "/Levels";

    [MenuItem("Assets/Create/Bus Jam/Level Data")]
    private static void CreateLevelData()
    {
        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder(LevelsFolder))
        {
            AssetDatabase.CreateFolder(ResourcesFolder, "Levels");
        }

        LevelData levelData = ScriptableObject.CreateInstance<LevelData>();

        string path = AssetDatabase.GenerateUniqueAssetPath(LevelsFolder + "/LevelData.asset");

        AssetDatabase.CreateAsset(levelData, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = levelData;
        EditorGUIUtility.PingObject(levelData);
    }
}