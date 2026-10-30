using System.Collections.Generic;
using UnityEngine;

public class LevelData : ScriptableObject
{
    public int LevelNumber = 1;
    public float TimeLimit = 60f;

    public Vector2Int GridSize;

    public List<GridCellData> GridCells = new List<GridCellData>();
    public List<BusData> Buses = new List<BusData>();
    public List<PassengerData> Passengers = new List<PassengerData>();
}