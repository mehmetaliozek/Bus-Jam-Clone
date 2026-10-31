using UnityEngine;

public enum GameColor
{
    Red = 0,
    Green = 1,
    Blue = 2,
    Yellow = 3,
    Purple = 4,
    Orange = 5,
    Cyan = 6,
    Pink = 7,
    Lime = 8,
    Gray = 9
}

public static class GameColorExtensions
{
    private static readonly Color[] CachedColors =
    {
        new Color32(239, 68, 68, 255),
        new Color32(34, 197, 94, 255),
        new Color32(59, 130, 246, 255),
        new Color32(234, 179, 8, 255),
        new Color32(168, 85, 247, 255),
        new Color32(249, 115, 22, 255),
        new Color32(6, 182, 212, 255),
        new Color32(236, 72, 153, 255),
        new Color32(132, 204, 22, 255),
        new Color32(100, 116, 139, 255)
    };

    public static Color ToUnityColor(this GameColor color)
    {
        int index = (int)color;
        return index >= 0 && index < CachedColors.Length ? CachedColors[index] : Color.white;
    }
}
