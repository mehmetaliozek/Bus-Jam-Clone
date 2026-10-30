using System.Collections.Generic;
using UnityEngine;

public class GridPathfinder
{
    private readonly Vector2Int[] _directions =
    {
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1)
    };

    private readonly Queue<GridCell> _cellsToCheck = new();
    private readonly HashSet<Vector2Int> _reachableCells = new();
    private readonly Dictionary<Vector2Int, Vector2Int> _nextTowardsExit = new();


    public void CalculateReachableCells(GridBuilder gridBuilder)
    {
        Clear();

        if (gridBuilder == null) return;

        foreach (GridCell exit in gridBuilder.ExitCells)
        {
            if (exit == null) continue;
            if (!_reachableCells.Add(exit.Coordinates)) continue;

            if (!exit.IsOccupied) _cellsToCheck.Enqueue(exit);
        }

        while (_cellsToCheck.Count > 0)
        {
            GridCell current = _cellsToCheck.Dequeue();

            foreach (Vector2Int direction in _directions)
            {
                Vector2Int coordinates = current.Coordinates + direction;

                if (!gridBuilder.TryGetCell(coordinates, out GridCell next)) continue;

                if (!_reachableCells.Add(coordinates)) continue;

                _nextTowardsExit.Add(coordinates, current.Coordinates);

                if (!next.IsOccupied) _cellsToCheck.Enqueue(next);
            }
        }
    }

    public bool CanReachExit(Passenger passenger, GridBuilder gridBuilder)
    {
        if (passenger == null || gridBuilder == null) return false;
        if (passenger.State != PassengerState.OnGrid) return false;

        if (!gridBuilder.TryGetCell(passenger.Coordinates, out GridCell cell))
            return false;

        return cell.Passenger == passenger && _reachableCells.Contains(passenger.Coordinates);
    }

    public bool TryGetPath(Passenger passenger, GridBuilder gridBuilder, out List<Vector3> path)
    {
        path = null;

        if (!CanReachExit(passenger, gridBuilder))
            return false;

        path = new List<Vector3>();

        Vector2Int coordinates = passenger.Coordinates;

        path.Add(gridBuilder.GetCell(coordinates).PassengerPosition);

        while (_nextTowardsExit.TryGetValue(coordinates, out Vector2Int next))
        {
            coordinates = next;
            path.Add(gridBuilder.GetCell(coordinates).PassengerPosition);
        }

        return true;
    }

    public void Clear()
    {
        _cellsToCheck.Clear();
        _reachableCells.Clear();
        _nextTowardsExit.Clear();
    }
}