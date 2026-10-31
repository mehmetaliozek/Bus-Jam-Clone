using System;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(SaveData data)
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception.Message);
        }
    }

    public static bool TryLoad(out SaveData data)
    {
        data = null;

        if (!File.Exists(SavePath)) return false;

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData loaded = new();
            JsonUtility.FromJsonOverwrite(json, loaded);

            if (loaded == null || loaded.LevelNumber < 1)
            {
                return false;
            }

            data = loaded;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(exception.Message);
            return false;
        }
    }
}
