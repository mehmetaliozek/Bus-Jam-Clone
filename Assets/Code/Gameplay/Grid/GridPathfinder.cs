using System.Collections.Generic;
using UnityEngine;

public class GridPathfinder
{
    private static readonly Vector2Int[] Directions = { Vector2Int.down, Vector2Int.right, Vector2Int.left, Vector2Int.up };

    private readonly Queue<GridCell> _cellsToCheck = new();
    private readonly HashSet<Vector2Int> _reachableCells = new();
    private readonly Dictionary<Vector2Int, Vector2Int> _nextTowardsExit = new();
    private readonly List<Vector3> _pathBuffer = new();

    public void CalculateReachableCells(GridBuilder gridBuilder)
    {
        Clear();
        if (gridBuilder == null) return;

        foreach (GridCell exit in gridBuilder.ExitCells)
        {
            if (exit != null && _reachableCells.Add(exit.Coordinates) && !exit.IsOccupied)
                _cellsToCheck.Enqueue(exit);
        }

        while (_cellsToCheck.Count > 0)
        {
            GridCell current = _cellsToCheck.Dequeue();

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int nextCoord = current.Coordinates + dir;
                if (gridBuilder.TryGetCell(nextCoord, out GridCell next) && _reachableCells.Add(nextCoord))
                {
                    _nextTowardsExit[nextCoord] = current.Coordinates;
                    if (!next.IsOccupied) _cellsToCheck.Enqueue(next);
                }
            }
        }
    }

    public bool CanReachExit(Passenger passenger, GridBuilder gridBuilder)
    {
        if (passenger == null || gridBuilder == null || passenger.State != PassengerState.OnGrid) return false;
        return gridBuilder.TryGetCell(passenger.Coordinates, out GridCell cell) &&
               cell.Passenger == passenger && _reachableCells.Contains(passenger.Coordinates);
    }

    public bool TryGetPath(Passenger passenger, GridBuilder gridBuilder, out List<Vector3> path)
    {
        path = null;
        if (!CanReachExit(passenger, gridBuilder)) return false;

        _pathBuffer.Clear();
        Vector2Int coord = passenger.Coordinates;
        _pathBuffer.Add(gridBuilder.GetCell(coord).PassengerPosition);

        while (_nextTowardsExit.TryGetValue(coord, out Vector2Int next))
        {
            coord = next;
            _pathBuffer.Add(gridBuilder.GetCell(coord).PassengerPosition);
        }

        path = _pathBuffer;
        return true;
    }

    public void Clear()
    {
        _cellsToCheck.Clear();
        _reachableCells.Clear();
        _nextTowardsExit.Clear();
    }
}
