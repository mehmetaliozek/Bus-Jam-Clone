using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int LevelNumber;
    public float RemainingTime;
    public LevelState State;
    public bool IsSoundEnabled = true;

    public bool RestartOnLoad;

    public List<PassengerData> GridPassengers = new();
    public List<WaitingPassengerSaveData> WaitingPassengers = new();
    public List<BusSaveData> Buses = new();
}

[Serializable]
public class WaitingPassengerSaveData
{
    public int SlotIndex;
    public GameColor Color;
}

[Serializable]
public class BusSaveData
{
    public GameColor Color;
    public int PassengerCount;
}