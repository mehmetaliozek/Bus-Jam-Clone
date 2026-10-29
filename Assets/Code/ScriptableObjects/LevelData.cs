using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Bus Jam/Level Data")]
public class LevelData : ScriptableObject
{
    public int LevelNumber = 1;
    public float TimeLimit = 60f;

    public Vector2Int GridSize;

    public List<GridCellData> GridCells = new List<GridCellData>();
    public List<BusData> Buses = new List<BusData>();
    public List<PassengerData> Passengers = new List<PassengerData>();
}