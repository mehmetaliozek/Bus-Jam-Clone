using System.Collections.Generic;
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [SerializeField] private GridBuilder _gridBuilder;
    [SerializeField] private PassengerSpawner _passengerSpawner;
    [SerializeField] private BusQueue _busQueue;
    [SerializeField] private WaitingSlots _waitingSlots;

    public GridBuilder GridBuilder => _gridBuilder;
    public PassengerSpawner PassengerSpawner => _passengerSpawner;
    public BusQueue BusQueue => _busQueue;
    public WaitingSlots WaitingSlots => _waitingSlots;

    public bool TryLoad(LevelData data, out string error)
    {
        if (!ValidateScene(out error)) return false;
        if (!ValidateData(data, out error)) return false;

        Clear();

        _gridBuilder.Build(data.GridSize, data.GridCells);
        _passengerSpawner.Spawn(data.Passengers, _gridBuilder);
        _busQueue.Build(data.Buses);

        return true;
    }

    private bool ValidateScene(out string error)
    {
        error = null;

        if (_gridBuilder == null || _passengerSpawner == null || _busQueue == null || _waitingSlots == null)
        {
            error = "LevelLoader references are missing.";
            return false;
        }

        if (!_gridBuilder.IsConfigured)
        {
            error = "GridBuilder requires a cell prefab and positive spacing.";
            return false;
        }

        if (!_passengerSpawner.IsConfigured)
        {
            error = "PassengerSpawner requires a passenger prefab.";
            return false;
        }

        if (!_busQueue.IsConfigured)
        {
            error = "BusQueue requires a configured bus prefab, unique seats, a boarding point, an exit point and a non-zero queue offset.";

            return false;
        }

        if (!_waitingSlots.IsConfigured)
        {
            error = "Waiting cells are missing or duplicated.";
            return false;
        }

        return true;
    }

    private bool ValidateData(LevelData data, out string error)
    {
        error = null;

        if (data == null)
        {
            error = "LevelData is missing.";
            return false;
        }

        if (data.LevelNumber < 1)
        {
            error = "Level number must be positive.";
            return false;
        }

        if (float.IsNaN(data.TimeLimit) || float.IsInfinity(data.TimeLimit) || data.TimeLimit < GameConstants.MinTimeLimit)
        {
            error = $"Time limit must be at least {GameConstants.MinTimeLimit} seconds.";

            return false;
        }

        if (data.GridSize.x <= 0 || data.GridSize.y <= 0)
        {
            error = "Grid size must be positive.";
            return false;
        }

        if (data.GridCells == null || data.Passengers == null || data.Buses == null)
        {
            error = "Level data lists cannot be null.";
            return false;
        }

        if (data.GridCells.Count == 0 || data.Passengers.Count == 0 || data.Buses.Count == 0)
        {
            error = "The level requires grid cells, passengers and buses.";
            return false;
        }

        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> passengerPositions = new HashSet<Vector2Int>();

        Dictionary<GameColor, int> passengerCounts = new Dictionary<GameColor, int>();

        Dictionary<GameColor, int> seatCounts = new Dictionary<GameColor, int>();

        for (int i = 0; i < data.GridCells.Count; i++)
        {
            GridCellData cell = data.GridCells[i];

            if (cell == null || !IsInsideGrid(cell.Coordinates, data.GridSize))
            {
                error = $"Grid cell {i} is invalid.";
                return false;
            }

            if (!cells.Add(cell.Coordinates))
            {
                error = $"Duplicate grid cell at {cell.Coordinates}.";
                return false;
            }
        }

        bool hasExit = false;

        foreach (Vector2Int coordinates in cells)
        {
            if (coordinates.y == 0)
            {
                hasExit = true;
                break;
            }
        }

        if (!hasExit)
        {
            error = "The level requires at least one active cell at y = 0.";
            return false;
        }

        for (int i = 0; i < data.Passengers.Count; i++)
        {
            PassengerData passenger = data.Passengers[i];

            if (passenger == null || !cells.Contains(passenger.Coordinates))
            {
                error = $"Passenger {i} must be placed on an active cell.";
                return false;
            }

            if (!passengerPositions.Add(passenger.Coordinates))
            {
                error = $"Multiple passengers at {passenger.Coordinates}.";
                return false;
            }

            AddCount(passengerCounts, passenger.Color, 1);
        }

        for (int i = 0; i < data.Buses.Count; i++)
        {
            BusData bus = data.Buses[i];

            if (bus == null)
            {
                error = $"Bus {i} is invalid.";
                return false;
            }

            AddCount(seatCounts, bus.Color, GameConstants.BusCapacity);
        }

        if (passengerCounts.Count != seatCounts.Count)
        {
            error = "Passenger colors and bus colors do not match.";
            return false;
        }

        foreach (KeyValuePair<GameColor, int> entry in passengerCounts)
        {
            seatCounts.TryGetValue(entry.Key, out int seats);

            if (entry.Value != seats)
            {
                error = $"{entry.Key}: {entry.Value} passengers, {seats} seats.";

                return false;
            }
        }

        return true;
    }

    private bool IsInsideGrid(Vector2Int coordinates, Vector2Int gridSize)
    {
        return coordinates.x >= 0 && coordinates.y >= 0 && coordinates.x < gridSize.x && coordinates.y < gridSize.y;
    }

    private void AddCount(Dictionary<GameColor, int> counts, GameColor color, int amount)
    {
        counts.TryGetValue(color, out int count);
        counts[color] = count + amount;
    }

    public void Clear()
    {
        if (_busQueue != null)
            _busQueue.Clear();

        if (_waitingSlots != null)
            _waitingSlots.Clear();

        if (_passengerSpawner != null)
            _passengerSpawner.Clear();

        if (_gridBuilder != null)
            _gridBuilder.Clear();
    }
}