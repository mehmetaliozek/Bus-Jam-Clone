using System;
using System.Collections.Generic;
using UnityEngine;

public class WaitingSlots : MonoBehaviour
{
    [SerializeField] private GridCell[] _slots = Array.Empty<GridCell>();
    public IReadOnlyList<GridCell> Slots => _slots;

    public int PassengerCount
    {
        get
        {
            if (_slots == null) return 0;

            int count = 0;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) continue;

                if (_slots[i].IsOccupied)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public bool IsFull => _slots != null && _slots.Length > 0 && PassengerCount == _slots.Length;

    public bool IsEmpty => PassengerCount == 0;

    public bool IsConfigured
    {
        get
        {
            if (_slots == null || _slots.Length == 0) return false;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) return false;

                for (int j = 0; j < i; j++)
                {
                    if (_slots[i] == _slots[j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public void Initialize()
    {
        Clear();
    }

    public bool ContainsPassenger(Passenger passenger)
    {
        if (passenger == null || _slots == null) return false;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null) continue;

            if (_slots[i].Passenger == passenger)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryAddPassengerToSlot(Passenger passenger)
    {
        if (passenger == null || _slots == null) return false;
        if (passenger.State != PassengerState.OnGrid) return false;
        if (ContainsPassenger(passenger)) return false;

        for (int i = 0; i < _slots.Length; i++)
        {
            GridCell slot = _slots[i];

            if (slot == null || slot.IsOccupied) continue;

            slot.SetPassenger(passenger);
            passenger.MoveToWaitingSlot(slot.PassengerPosition);

            return true;
        }

        return false;
    }

    public bool TryGetPassenger(GameColor color, out Passenger passenger)
    {
        passenger = null;

        if (_slots == null) return false;

        for (int i = 0; i < _slots.Length; i++)
        {
            GridCell slot = _slots[i];

            if (slot == null || !slot.IsOccupied) continue;
            if (slot.Passenger.Color != color) continue;

            passenger = slot.Passenger;
            return true;
        }

        return false;
    }

    public void RemovePassengerFromSlot(Passenger passenger)
    {
        if (passenger == null || _slots == null) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            GridCell slot = _slots[i];

            if (slot == null) continue;
            if (slot.Passenger != passenger) continue;

            slot.RemovePassenger(passenger);
            return;
        }
    }

    public void Clear()
    {
        if (_slots == null) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            GridCell slot = _slots[i];

            if (slot == null) continue;

            slot.RemovePassenger(slot.Passenger);
        }
    }
}