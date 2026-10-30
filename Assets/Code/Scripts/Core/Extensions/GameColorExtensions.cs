using UnityEngine;

public static class GameColorExtensions
{
    public static Color ToUnityColor(this GameColor color)
    {
        return color switch
        {
            GameColor.Red => new Color32(239, 68, 68, 255),
            GameColor.Green => new Color32(34, 197, 94, 255),
            GameColor.Blue => new Color32(59, 130, 246, 255),
            GameColor.Yellow => new Color32(234, 179, 8, 255),
            GameColor.Purple => new Color32(168, 85, 247, 255),
            GameColor.Orange => new Color32(249, 115, 22, 255),
            GameColor.Cyan => new Color32(6, 182, 212, 255),
            GameColor.Pink => new Color32(236, 72, 153, 255),
            GameColor.Lime => new Color32(132, 204, 22, 255),
            GameColor.Gray => new Color32(100, 116, 139, 255),
            _ => new Color32(255, 255, 255, 255)
        };
    }
}