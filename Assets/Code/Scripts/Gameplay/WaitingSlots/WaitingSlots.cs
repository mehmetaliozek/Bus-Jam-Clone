using System;
using System.Collections.Generic;
using UnityEngine;

public class WaitingSlots : MonoBehaviour
{
    [SerializeField] private GridCell[] _slots = Array.Empty<GridCell>();

    public IReadOnlyList<GridCell> Slots => _slots;

    public bool IsEmpty
    {
        get
        {
            if (_slots == null) return true;

            foreach (GridCell slot in _slots)
            {
                if (slot != null && slot.IsOccupied)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool AllPassengersArrived
    {
        get
        {
            if (_slots == null || _slots.Length == 0)
                return false;

            foreach (GridCell slot in _slots)
            {
                if (slot == null || slot.Passenger == null)
                    return false;

                if (slot.Passenger.State != PassengerState.Waiting)
                    return false;
            }

            return true;
        }
    }

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

    public bool TryReserveSlot(Passenger passenger, out GridCell reservedSlot)
    {
        reservedSlot = null;

        if (passenger == null || _slots == null) return false;
        if (passenger.State != PassengerState.OnGrid) return false;
        if (ContainsPassenger(passenger)) return false;

        for (int i = 0; i < _slots.Length; i++)
        {
            GridCell slot = _slots[i];

            if (slot == null || slot.IsOccupied) continue;

            slot.SetPassenger(passenger);
            reservedSlot = slot;

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

            Passenger candidate = slot.Passenger;

            if (candidate.State != PassengerState.Waiting) continue;
            if (candidate.Color != color) continue;

            passenger = candidate;
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