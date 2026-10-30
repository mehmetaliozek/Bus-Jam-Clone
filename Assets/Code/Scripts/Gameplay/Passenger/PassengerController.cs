using System;
using System.Collections.Generic;
using UnityEngine;

public class PassengerController : MonoBehaviour
{
    private sealed class Trip
    {
        public Bus Bus;
        public GridCell Slot;
        public Transform Seat;
    }

    public event Action LevelCompleted;
    public event Action<string> LevelFailed;

    [SerializeField] private Transform _walkway;

    private readonly Dictionary<Passenger, Trip> _trips = new();
    private readonly GridPathfinder _gridPathfinder = new();

    private GridBuilder _gridBuilder;
    private PassengerSpawner _passengerSpawner;
    private BusQueue _busQueue;
    private WaitingSlots _waitingSlots;

    private bool _isInitialized;
    private bool _inputEnabled;
    private bool _isResolving;
    private bool _isCancelling;

    private bool _gridDirty = true;

    private int _remainingPassengers;

    private bool CanProcess => _isInitialized && _inputEnabled && isActiveAndEnabled;


    public void Initialize(GridBuilder gridBuilder, PassengerSpawner passengerSpawner, BusQueue busQueue, WaitingSlots waitingSlots)
    {
        _gridBuilder = gridBuilder;
        _passengerSpawner = passengerSpawner;
        _busQueue = busQueue;
        _waitingSlots = waitingSlots;

        _inputEnabled = false;
        _gridDirty = true;

        _remainingPassengers = 0;

        foreach (Passenger passenger in _passengerSpawner.Passengers)
        {
            if (passenger.State != PassengerState.Boarded)
            {
                _remainingPassengers++;
            }
        }

        _isInitialized = true;

        foreach (Passenger passenger in _passengerSpawner.Passengers)
        {
            passenger.Clicked += HandlePassengerClicked;
            passenger.Movement.MovementCancelled += HandleMovementCancelled;
        }

        _busQueue.ActiveBusChanged += ResolveWaitingPassengers;

        RefreshPassengerMovement();
    }

    public SaveData CreateSave(int levelNumber, float remainingTime, LevelState state)
    {
        SaveData save = new SaveData
        {
            LevelNumber = levelNumber,
            RemainingTime = remainingTime,
            State = state
        };

        foreach (Passenger passenger in _passengerSpawner.Passengers)
        {
            if (passenger.State != PassengerState.OnGrid)
                continue;

            save.GridPassengers.Add(new PassengerData
            {
                Color = passenger.Color,
                Coordinates = passenger.Coordinates
            });
        }

        for (int i = 0; i < _waitingSlots.Slots.Count; i++)
        {
            Passenger passenger = _waitingSlots.Slots[i].Passenger;

            if (passenger == null)
                continue;

            save.WaitingPassengers.Add(new WaitingPassengerSaveData
            {
                SlotIndex = i,
                Color = passenger.Color
            });
        }

        List<Bus> buses = _busQueue.GetRemainingBuses();

        foreach (Bus bus in buses)
        {
            int passengerCount = bus.PassengerCount;

            foreach (Trip trip in _trips.Values)
            {
                if (trip.Bus == bus)
                {
                    passengerCount++;
                }
            }

            save.Buses.Add(new BusSaveData
            {
                Color = bus.Color,
                PassengerCount = passengerCount
            });
        }

        while (save.Buses.Count > 0 && save.Buses[0].PassengerCount == GameConstants.BusCapacity)
        {
            save.Buses.RemoveAt(0);
        }

        if (save.GridPassengers.Count == 0 && save.WaitingPassengers.Count == 0 && save.Buses.Count == 0)
        {
            save.State = LevelState.Completed;
        }

        return save;
    }

    public void SetInputEnabled(bool isEnabled)
    {
        _inputEnabled = isEnabled;

        UpdateMovementPause();

        if (CanProcess)
        {
            ResolveWaitingPassengers();
        }
        else
        {
            RefreshPassengerMovement();
        }
    }

    private void OnEnable()
    {
        if (!_isInitialized) return;

        UpdateMovementPause();

        if (CanProcess)
        {
            ResolveWaitingPassengers();
        }
        else
        {
            RefreshPassengerMovement();
        }
    }

    private void OnDisable()
    {
        UpdateMovementPause();
        RefreshPassengerMovement();
    }

    private void UpdateMovementPause()
    {
        bool paused = !CanProcess;

        if (_busQueue != null) _busQueue.SetPaused(paused);

        foreach (Passenger passenger in _trips.Keys)
        {
            if (passenger != null) passenger.Movement.SetPaused(paused);
        }
    }
    private void UpdateReachabilityIfNeeded()
    {
        if (!_isInitialized || !_gridDirty) return;

        _gridPathfinder.CalculateReachableCells(_gridBuilder);
        _gridDirty = false;
    }

    private void HandlePassengerClicked(Passenger passenger)
    {
        if (!CanProcess || passenger == null || !passenger.CanMove) return;

        if (_walkway == null)
        {
            Fail("PassengerController requires a walkway reference.");
            return;
        }

        UpdateReachabilityIfNeeded();

        if (!_gridPathfinder.TryGetPath(passenger, _gridBuilder, out List<Vector3> route)) return;

        Bus bus = _busQueue.ActiveBus;

        if (CanReserveBus(passenger, bus))
        {
            if (!bus.HasBoardingPoint)
            {
                Fail("Bus requires a boarding point.");
                return;
            }

            AppendDestination(route, bus.BoardingPosition);

            StartTrip(passenger, new Trip { Bus = bus }, route);
        }
        else if (_waitingSlots.TryReserveSlot(passenger, out GridCell slot))
        {
            AppendDestination(route, slot.PassengerPosition);

            StartTrip(passenger, new Trip { Slot = slot }, route);
        }

        RefreshPassengerMovement();
        CheckResult();
    }

    private bool CanReserveBus(Passenger passenger, Bus bus)
    {
        if (!_busQueue.IsBoardingOpen) return false;

        if (bus == null || bus != _busQueue.ActiveBus) return false;

        if (passenger.Color != bus.Color) return false;

        int reservedSeats = 0;

        foreach (Trip trip in _trips.Values)
        {
            if (trip.Bus == bus) reservedSeats++;
        }

        return bus.PassengerCount + reservedSeats < GameConstants.BusCapacity;
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
                _gridDirty = true;
            }
        }
        else if (previousState == PassengerState.Waiting)
        {
            _waitingSlots.RemovePassengerFromSlot(passenger);
        }

        passenger.BeginMoving();

        passenger.Movement.Move(route, () => HandleArrival(passenger, trip));
    }

    private void HandleArrival(Passenger passenger, Trip trip)
    {
        if (!CanProcess) return;

        if (!_trips.TryGetValue(passenger, out Trip activeTrip)) return;

        if (!ReferenceEquals(activeTrip, trip)) return;

        if (trip.Bus != null)
        {
            if (!trip.Bus.TryReserveSeat(passenger, out Transform seat))
            {
                Fail("No seat available for the reserved passenger.");
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

            ResolveWaitingPassengers();
            return;
        }

        Fail("Passenger destination is no longer valid.");
    }

    private void HandleSeated(Passenger passenger, Trip trip)
    {
        if (!CanProcess) return;

        if (!_trips.TryGetValue(passenger, out Trip activeTrip)) return;

        if (!ReferenceEquals(activeTrip, trip)) return;

        if (trip.Bus == null || !trip.Bus.TryCompleteSeating(passenger, trip.Seat))
        {
            Fail("Passenger could not complete seating.");
            return;
        }

        passenger.Board(trip.Seat);

        _trips.Remove(passenger);
        _remainingPassengers--;

        _busQueue.AdvanceIfFull();

        ResolveWaitingPassengers();
    }

    private void ResolveWaitingPassengers()
    {
        if (!CanProcess || _isResolving) return;

        _isResolving = true;

        try
        {
            Bus bus = _busQueue.ActiveBus;

            while (CanProcess && bus != null)
            {
                if (!_waitingSlots.TryGetPassenger(bus.Color, out Passenger passenger)) break;

                if (!CanReserveBus(passenger, bus)) break;

                if (_walkway == null || !bus.HasBoardingPoint)
                {
                    Fail("Walkway or bus boarding point is missing.");
                    return;
                }

                List<Vector3> route = new List<Vector3>
                {
                    passenger.transform.position
                };

                AppendDestination(route, bus.BoardingPosition);

                StartTrip(passenger, new Trip { Bus = bus }, route);
            }
        }
        finally
        {
            _isResolving = false;
        }

        RefreshPassengerMovement();
        CheckResult();
    }

    private void AppendDestination(List<Vector3> route, Vector3 destination)
    {
        Vector3 start = route[route.Count - 1];

        Vector3 localStart = _gridBuilder.transform.InverseTransformPoint(start);
        Vector3 localTarget = _gridBuilder.transform.InverseTransformPoint(destination);
        Vector3 localWalkway = _gridBuilder.transform.InverseTransformPoint(_walkway.position);

        route.Add(_gridBuilder.transform.TransformPoint(new Vector3(localStart.x, localStart.y, localWalkway.z)));

        route.Add(_gridBuilder.transform.TransformPoint(new Vector3(localTarget.x, localTarget.y, localWalkway.z)));

        route.Add(destination);
    }

    private void RefreshPassengerMovement()
    {
        if (!_isInitialized) return;

        bool canProcess = CanProcess;

        if (canProcess)
        {
            UpdateReachabilityIfNeeded();
        }

        foreach (Passenger passenger in _passengerSpawner.Passengers)
        {
            if (passenger == null) continue;

            bool canMove = canProcess && _gridPathfinder.CanReachExit(passenger, _gridBuilder);

            passenger.SetCanMove(canMove);
        }
    }

    private void CheckResult()
    {
        if (!CanProcess) return;
        if (_busQueue.IsTransitioning) return;

        if (_waitingSlots.AllPassengersArrived)
        {
            Fail("All waiting slots are occupied.");
            return;
        }

        if (_trips.Count > 0) return;

        if (_remainingPassengers == 0 && _busQueue.IsEmpty && _waitingSlots.IsEmpty)
        {
            LevelCompleted?.Invoke();
        }
    }

    private void Fail(string reason)
    {
        if (!_isInitialized || !_inputEnabled) return;

        LevelFailed?.Invoke(reason);
    }

    private void HandleMovementCancelled(PassengerMovement movement)
    {
        if (_isCancelling || !_isInitialized) return;

        Passenger passenger = movement.GetComponent<Passenger>();

        if (passenger != null && _trips.ContainsKey(passenger))
            Fail("A passenger movement was interrupted.");
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

                if (passenger != null) passenger.Movement.Cancel();
            }

            _trips.Clear();
        }
        finally
        {
            _isCancelling = false;
        }
    }

    public void Clear()
    {
        _inputEnabled = false;

        CancelMovements();

        if (_passengerSpawner != null)
        {
            foreach (Passenger passenger in _passengerSpawner.Passengers)
            {
                if (passenger == null) continue;

                passenger.Clicked -= HandlePassengerClicked;
                passenger.SetCanMove(false);

                if (passenger.Movement != null)
                {
                    passenger.Movement.MovementCancelled -= HandleMovementCancelled;
                }
            }
        }

        if (_busQueue != null) _busQueue.ActiveBusChanged -= ResolveWaitingPassengers;

        _gridPathfinder.Clear();
        _gridDirty = true;

        _isInitialized = false;
        _isResolving = false;
        _remainingPassengers = 0;

        _gridBuilder = null;
        _passengerSpawner = null;
        _busQueue = null;
        _waitingSlots = null;
    }

    private void OnDestroy()
    {
        Clear();
    }
}