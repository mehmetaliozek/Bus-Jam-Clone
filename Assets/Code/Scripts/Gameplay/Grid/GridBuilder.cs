using System.Collections.Generic;
using UnityEngine;

public class GridBuilder : MonoBehaviour
{
    [SerializeField] private GridCell _cellPrefab;
    [SerializeField] private float _cellSpacing = 2f;

    private readonly Dictionary<Vector2Int, GridCell> _cells = new();
    private readonly List<GridCell> _exitCells = new();

    private PrefabPool<GridCell> _pool;

    public IReadOnlyList<GridCell> ExitCells => _exitCells;

    public bool IsConfigured => _cellPrefab != null && _cellSpacing > 0f;


    private void Awake()
    {
        if (_cellPrefab != null) EnsurePool();
    }

    private void EnsurePool()
    {
        if (_pool != null) return;

        _pool = new PrefabPool<GridCell>(
            _cellPrefab,
            transform,
            initialCount: 30,
            maxSize: 100
        );
    }

    public void Build(Vector2Int gridSize, IReadOnlyList<GridCellData> cells)
    {
        EnsurePool();

        for (int i = 0; i < cells.Count; i++)
        {
            GridCellData data = cells[i];

            Vector3 position = GetCellPosition(data.Coordinates, gridSize);

            GridCell cell = _pool.Get(
                transform,
                position,
                transform.rotation
            );

            cell.Initialize(data);

            _cells.Add(data.Coordinates, cell);

            if (data.Coordinates.y == 0)
            {
                _exitCells.Add(cell);
            }
        }
    }

    private Vector3 GetCellPosition(Vector2Int coordinates, Vector2Int gridSize)
    {
        float halfWidth = (gridSize.x - 1) * _cellSpacing * 0.5f;

        Vector3 localPosition = new Vector3(
            coordinates.x * _cellSpacing - halfWidth,
            0f,
            -coordinates.y * _cellSpacing
        );

        return transform.TransformPoint(localPosition);
    }

    public GridCell GetCell(Vector2Int coordinates)
    {
        return _cells[coordinates];
    }

    public bool TryGetCell(Vector2Int coordinates, out GridCell cell)
    {
        return _cells.TryGetValue(coordinates, out cell);
    }

    public void Clear()
    {
        _exitCells.Clear();

        foreach (GridCell cell in _cells.Values)
        {
            if (cell == null) continue;

            cell.RemovePassenger(cell.Passenger);
            _pool.Release(cell);
        }

        _cells.Clear();
    }

    private void OnDestroy()
    {
        _pool?.Dispose();
    }
}