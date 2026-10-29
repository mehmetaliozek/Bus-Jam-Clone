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

    private readonly Queue<GridCell> _cellsToCheck = new Queue<GridCell>();

    private readonly HashSet<Vector2Int> _reachableCells = new HashSet<Vector2Int>();

    public void CalculateReachableCells(GridBuilder gridBuilder)
    {
        Clear();

        if (gridBuilder == null) return;

        for (int i = 0; i < gridBuilder.ExitCells.Count; i++)
        {
            VisitCell(gridBuilder.ExitCells[i]);
        }

        while (_cellsToCheck.Count > 0)
        {
            GridCell currentCell = _cellsToCheck.Dequeue();

            for (int i = 0; i < _directions.Length; i++)
            {
                Vector2Int coordinates = currentCell.Coordinates + _directions[i];

                if (!gridBuilder.TryGetCell(coordinates, out GridCell nextCell))
                {
                    continue;
                }

                VisitCell(nextCell);
            }
        }
    }

    private void VisitCell(GridCell cell)
    {
        if (cell == null) return;

        if (!_reachableCells.Add(cell.Coordinates)) return;

        if (cell.IsOccupied) return;

        _cellsToCheck.Enqueue(cell);
    }

    public bool CanReachExit(Passenger passenger, GridBuilder gridBuilder)
    {
        if (passenger == null || gridBuilder == null) return false;
        if (passenger.State != PassengerState.OnGrid) return false;

        if (!gridBuilder.TryGetCell(passenger.Coordinates, out GridCell cell))
        {
            return false;
        }

        if (cell.Passenger != passenger) return false;

        return _reachableCells.Contains(passenger.Coordinates);
    }

    public void Clear()
    {
        _cellsToCheck.Clear();
        _reachableCells.Clear();
    }
}