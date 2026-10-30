using System.Collections.Generic;
using UnityEngine;

public class PassengerSpawner : MonoBehaviour
{
    [SerializeField] private Passenger _passengerPrefab;

    private readonly List<Passenger> _passengers = new();

    private PrefabPool<Passenger> _pool;

    public IReadOnlyList<Passenger> Passengers => _passengers;

    public bool IsConfigured => _passengerPrefab != null;


    private void Awake()
    {
        if (IsConfigured) EnsurePool();
    }

    private void EnsurePool()
    {
        if (_pool != null) return;

        _pool = new PrefabPool<Passenger>(
            _passengerPrefab,
            transform,
            initialCount: 30,
            maxSize: 100
        );
    }

    public void Spawn(IReadOnlyList<PassengerData> passengers, GridBuilder gridBuilder)
    {
        EnsurePool();

        for (int i = 0; i < passengers.Count; i++)
        {
            PassengerData data = passengers[i];
            GridCell cell = gridBuilder.GetCell(data.Coordinates);

            Passenger passenger = _pool.Get(
                transform,
                cell.PassengerPosition,
                gridBuilder.transform.rotation
            );

            passenger.Initialize(data);

            cell.SetPassenger(passenger);
            _passengers.Add(passenger);
        }
    }

    public Passenger SpawnAt(GameColor color, Vector3 position)
    {
        EnsurePool();

        Passenger passenger = _pool.Get(
            transform,
            position,
            Quaternion.identity
        );

        passenger.Initialize(new PassengerData { Color = color, Coordinates = Vector2Int.zero });

        _passengers.Add(passenger);

        return passenger;
    }

    public void Clear()
    {
        foreach (Passenger passenger in _passengers)
        {
            if (passenger == null) continue;

            passenger.SetCanMove(false);
            _pool.Release(passenger);
        }

        _passengers.Clear();
    }

    private void OnDestroy()
    {
        _pool?.Dispose();
    }
}