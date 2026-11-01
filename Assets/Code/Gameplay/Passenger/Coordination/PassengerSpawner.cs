using System.Collections.Generic;
using UnityEngine;

public class PassengerSpawner : MonoBehaviour
{
    [SerializeField] private Passenger _passengerPrefab;

    private readonly List<Passenger> _passengers = new();
    private readonly Dictionary<Passenger, PrefabPool<Passenger>> _poolsByPrefab = new();

    public IReadOnlyList<Passenger> Passengers => _passengers;

    public bool IsConfigured => _passengerPrefab != null;

    private void Awake()
    {
        if (IsConfigured) GetOrCreatePool(_passengerPrefab);
    }

    private PrefabPool<Passenger> GetOrCreatePool(Passenger prefab)
    {
        if (prefab == null) return null;

        if (_poolsByPrefab.TryGetValue(prefab, out PrefabPool<Passenger> existingPool))
        {
            return existingPool;
        }

        int initialCount = prefab == _passengerPrefab ? 30 : 4;
        PrefabPool<Passenger> newPool = new(
            prefab,
            transform,
            initialCount: initialCount,
            maxSize: 100
        );

        _poolsByPrefab[prefab] = newPool;
        return newPool;
    }

    public void Spawn(IReadOnlyList<PassengerData> passengers, GridBuilder gridBuilder)
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            PassengerData data = passengers[i];
            GridCell cell = gridBuilder.GetCell(data.Coordinates);

            Passenger prefabToUse = data.Type != null && data.Type.PrefabOverride != null ? data.Type.PrefabOverride : _passengerPrefab;

            PrefabPool<Passenger> pool = GetOrCreatePool(prefabToUse);
            if (pool == null) continue;

            Passenger passenger = pool.Get(
                transform,
                cell.PassengerPosition,
                gridBuilder.transform.rotation
            );

            passenger.SourcePrefab = prefabToUse;
            passenger.Initialize(data);

            cell.SetPassenger(passenger);
            _passengers.Add(passenger);
        }
    }

    public void Clear()
    {
        foreach (Passenger passenger in _passengers)
        {
            if (passenger == null) continue;

            passenger.ResetMechanicState();
            passenger.SetCanMove(false);

            Passenger prefabKey = passenger.SourcePrefab != null ? passenger.SourcePrefab : _passengerPrefab;
            if (prefabKey != null && _poolsByPrefab.TryGetValue(prefabKey, out PrefabPool<Passenger> pool))
            {
                pool.Release(passenger);
            }
            else
            {
                Destroy(passenger.gameObject);
            }
        }

        _passengers.Clear();
    }

    private void OnDestroy()
    {
        foreach (PrefabPool<Passenger> pool in _poolsByPrefab.Values)
        {
            pool?.Dispose();
        }

        _poolsByPrefab.Clear();
    }
}
