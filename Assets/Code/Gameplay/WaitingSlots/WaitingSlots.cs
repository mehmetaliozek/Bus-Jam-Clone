using System;
using System.Collections.Generic;
using UnityEngine;

public class WaitingSlots : MonoBehaviour
{
    [SerializeField] private GridCell[] _slots = Array.Empty<GridCell>();

    public IReadOnlyList<GridCell> Slots => _slots;

    public bool IsEmpty => _slots == null || Array.TrueForAll(_slots, s => s == null || !s.IsOccupied);

    public bool AllPassengersArrived => _slots != null && _slots.Length > 0 && Array.TrueForAll(_slots, s => s != null && s.Passenger != null && s.Passenger.State == PassengerState.Waiting);


    private bool ContainsPassenger(Passenger passenger) => passenger != null && _slots != null && Array.Exists(_slots, s => s != null && s.Passenger == passenger);

    public bool TryReserveSlot(Passenger passenger, out GridCell reservedSlot)
    {
        reservedSlot = null;

        if (passenger == null || _slots == null || passenger.State != PassengerState.OnGrid || ContainsPassenger(passenger)) return false;

        foreach (GridCell slot in _slots)
        {
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

        foreach (GridCell slot in _slots)
        {
            if (slot != null && slot.IsOccupied && slot.Passenger.State == PassengerState.Waiting && slot.Passenger.Color == color)
            {
                passenger = slot.Passenger;
                return true;
            }
        }

        return false;
    }

    public void RemovePassengerFromSlot(Passenger passenger)
    {
        if (passenger == null || _slots == null) return;

        foreach (GridCell slot in _slots)
        {
            if (slot != null && slot.Passenger == passenger)
            {
                slot.RemovePassenger(passenger);
                return;
            }
        }
    }

    public void Clear()
    {
        if (_slots == null) return;

        foreach (GridCell slot in _slots)
        {
            slot.RemovePassenger(slot.Passenger);
        }
    }
}
