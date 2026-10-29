using System;
using System.Collections.Generic;
using UnityEngine;

public class BusQueue : MonoBehaviour
{
    public event Action ActiveBusChanged;

    [SerializeField] private Bus _busPrefab;
    [SerializeField] private Vector3 _queueOffset = new Vector3(8f, 0f, 0f);

    private Queue<Bus> _busQueue = new Queue<Bus>();

    public Bus ActiveBus => _busQueue.Count > 0 ? _busQueue.Peek() : null;

    public bool IsEmpty => _busQueue.Count == 0;
    public bool IsConfigured => _busPrefab != null;

    public void Build(IReadOnlyList<BusData> buses)
    {
        Clear();

        for (int i = 0; i < buses.Count; i++)
        {
            Vector3 position = transform.TransformPoint(_queueOffset * i);

            Bus bus = Instantiate(_busPrefab, position, transform.rotation, transform);

            bus.Initialize(buses[i]);

            _busQueue.Enqueue(bus);
        }

        ActiveBusChanged?.Invoke();
    }

    public bool TryAddPassengerToBus(Passenger passenger)
    {
        Bus bus = ActiveBus;

        if (bus == null) return false;

        return bus.TryAddPassenger(passenger);
    }

    public void AdvanceIfFull()
    {
        Bus bus = ActiveBus;

        if (bus == null || !bus.IsFull) return;

        _busQueue.Dequeue();

        bus.gameObject.SetActive(false);
        Destroy(bus.gameObject);

        UpdateBusPositions();
        ActiveBusChanged?.Invoke();
    }

    private void UpdateBusPositions()
    {
        int index = 0;

        foreach (Bus bus in _busQueue)
        {
            bus.transform.position = transform.TransformPoint(_queueOffset * index);

            index++;
        }
    }

    public void Clear()
    {
        while (_busQueue.Count > 0)
        {
            Bus bus = _busQueue.Dequeue();

            if (bus == null) continue;

            bus.gameObject.SetActive(false);
            Destroy(bus.gameObject);
        }
    }
}