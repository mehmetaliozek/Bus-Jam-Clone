using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardingCoordinator
{
    public sealed class Trip
    {
        public Bus Bus;
        public GridCell Slot;
        public Transform Seat;
    }

    public event Action PassengerBoarded;
    public event Action<string> Failed;
    public event Action TripsChanged;

    private readonly Dictionary<Passenger, Trip> _trips = new();

    private GridBuilder _gridBuilder;
    private WaitingSlots _waitingSlots;
    private BusQueue _busQueue;
    private Transform _walkway;

    private bool _isResolving;
    private bool _isCancelling;

    public int ActiveTripCount => _trips.Count;

    public void Initialize(GridBuilder gridBuilder, WaitingSlots waitingSlots, BusQueue busQueue, Transform walkway)
    {
        _gridBuilder = gridBuilder;
        _waitingSlots = waitingSlots;
        _busQueue = busQueue;
        _walkway = walkway;
    }

    private int GetReservedSeatCount(Bus bus)
    {
        if (bus == null) return 0;

        int count = 0;
        foreach (Trip trip in _trips.Values)
        {
            if (trip.Bus == bus) count++;
        }
        return count;
    }

    public bool CanReserveBus(Passenger passenger, Bus bus)
    {
        if (_busQueue == null || !_busQueue.IsBoardingOpen) return false;
        if (bus == null || bus != _busQueue.ActiveBus) return false;
        if (passenger.Color != bus.Color) return false;

        int reservedSeats = GetReservedSeatCount(bus);
        return bus.PassengerCount + reservedSeats < GameConstants.BusCapacity;
    }

    public void StartTripToBus(Passenger passenger, Bus bus, List<Vector3> route)
    {
        AppendDestination(route, bus.BoardingPosition, _gridBuilder, _walkway);
        StartTrip(passenger, new Trip { Bus = bus }, route);
    }

    public void StartTripToSlot(Passenger passenger, GridCell slot, List<Vector3> route)
    {
        AppendDestination(route, slot.PassengerPosition, _gridBuilder, _walkway);
        StartTrip(passenger, new Trip { Slot = slot }, route);
    }

    private static void AppendDestination(List<Vector3> route, Vector3 destination, GridBuilder gridBuilder, Transform walkway)
    {
        if (route == null || route.Count == 0 || gridBuilder == null || walkway == null) return;

        Vector3 start = route[route.Count - 1];

        Vector3 localStart = gridBuilder.transform.InverseTransformPoint(start);
        Vector3 localTarget = gridBuilder.transform.InverseTransformPoint(destination);
        Vector3 localWalkway = gridBuilder.transform.InverseTransformPoint(walkway.position);

        route.Add(gridBuilder.transform.TransformPoint(new Vector3(localStart.x, localStart.y, localWalkway.z)));
        route.Add(gridBuilder.transform.TransformPoint(new Vector3(localTarget.x, localTarget.y, localWalkway.z)));
        route.Add(destination);
    }

    private void StartTrip(Passenger passenger, Trip trip, List<Vector3> route)
    {
        PassengerState previousState = passenger.State;

        _trips.Add(passenger, trip);

        if (previousState == PassengerState.OnGrid)
        {
            if (_gridBuilder.TryGetCell(passenger.Coordinates, out GridCell cell))
            {
                cell.RemovePassenger(passenger);
            }
        }
        else if (previousState == PassengerState.Waiting)
        {
            _waitingSlots.RemovePassengerFromSlot(passenger);
        }

        passenger.BeginMoving();
        passenger.Movement.Move(route, () => HandleArrival(passenger, trip));

        TripsChanged?.Invoke();
    }

    private void HandleArrival(Passenger passenger, Trip trip)
    {
        if (!_trips.TryGetValue(passenger, out Trip activeTrip)) return;
        if (!ReferenceEquals(activeTrip, trip)) return;

        if (trip.Bus != null)
        {
            if (!trip.Bus.TryReserveSeat(passenger, out Transform seat))
            {
                Failed?.Invoke("No seat available for the reserved passenger.");
                return;
            }

            trip.Seat = seat;
            passenger.Movement.MoveToSeat(seat, () => HandleSeated(passenger, trip));
            return;
        }

        if (trip.Slot != null && trip.Slot.Passenger == passenger)
        {
            _trips.Remove(passenger);
            passenger.ArriveAtWaitingSlot();
            TripsChanged?.Invoke();
            return;
        }

        Failed?.Invoke("Passenger destination is no longer valid.");
    }

    private void HandleSeated(Passenger passenger, Trip trip)
    {
        if (!_trips.TryGetValue(passenger, out Trip activeTrip)) return;
        if (!ReferenceEquals(activeTrip, trip)) return;

        if (trip.Bus == null || !trip.Bus.TryCompleteSeating(passenger, trip.Seat))
        {
            Failed?.Invoke("Passenger could not complete seating.");
            return;
        }

        passenger.Board(trip.Seat);
        _trips.Remove(passenger);

        PassengerBoarded?.Invoke();
        GameEvents.TriggerPassengerBoarded();

        _busQueue.AdvanceIfFull();

        TripsChanged?.Invoke();
    }

    public void ResolveWaitingPassengers(Func<bool> canProcessPredicate)
    {
        if (canProcessPredicate != null && !canProcessPredicate()) return;
        if (_isResolving) return;

        _isResolving = true;

        try
        {
            Bus bus = _busQueue != null ? _busQueue.ActiveBus : null;

            while ((canProcessPredicate == null || canProcessPredicate()) && bus != null)
            {
                if (!_waitingSlots.TryGetPassenger(bus.Color, out Passenger passenger)) break;
                if (!CanReserveBus(passenger, bus)) break;

                if (_walkway == null || !bus.HasBoardingPoint)
                {
                    Failed?.Invoke("Walkway or bus boarding point is missing.");
                    return;
                }

                List<Vector3> route = new List<Vector3>
                {
                    passenger.transform.position
                };

                StartTripToBus(passenger, bus, route);
            }
        }
        finally
        {
            _isResolving = false;
        }
    }

    public void UpdateMovementPause(bool paused)
    {
        if (_busQueue != null) _busQueue.SetPaused(paused);

        foreach (Passenger passenger in _trips.Keys)
        {
            if (passenger != null && passenger.Movement != null)
            {
                passenger.Movement.SetPaused(paused);
            }
        }
    }

    public bool HasActiveTripFor(Passenger passenger)
    {
        return _trips.ContainsKey(passenger);
    }

    public void CancelMovements()
    {
        if (_isCancelling) return;
        _isCancelling = true;

        try
        {
            if (_busQueue != null) _busQueue.StopTransition();

            foreach (KeyValuePair<Passenger, Trip> entry in _trips)
            {
                Passenger passenger = entry.Key;
                Trip trip = entry.Value;

                if (trip.Slot != null) trip.Slot.RemovePassenger(passenger);
                if (trip.Bus != null) trip.Bus.ReleaseSeatReservation(passenger);
                if (passenger != null && passenger.Movement != null) passenger.Movement.Cancel();
            }

            _trips.Clear();
            TripsChanged?.Invoke();
        }
        finally
        {
            _isCancelling = false;
        }
    }

    public void Clear()
    {
        CancelMovements();
        _isResolving = false;
        _gridBuilder = null;
        _waitingSlots = null;
        _busQueue = null;
        _walkway = null;
    }
}

