using System.Collections.Generic;
using UnityEngine;

public class PassengerController : MonoBehaviour
{
    [SerializeField] private GridBuilder _gridBuilder;
    [SerializeField] private PassengerSpawner _passengerSpawner;
    [SerializeField] private BusQueue _busQueue;
    [SerializeField] private WaitingSlots _waitingSlots;

    private readonly BoardingCoordinator _boardingCoordinator = new();
    private readonly GridPathfinder _gridPathfinder = new();

    private bool _isInitialized;
    private bool _inputEnabled;
    private bool _gridDirty = true;
    private int _remainingPassengers;

    private bool CanProcess => _isInitialized && _inputEnabled && isActiveAndEnabled;

    private void OnDestroy()
    {
        Clear();
    }

    private void OnEnable()
    {
        GameEvents.LevelLoaded += HandleLevelLoaded;
        GameEvents.LevelStateChanged += HandleLevelStateChanged;
        GameEvents.ActiveBusChanged += ResolveWaitingPassengers;

        if (_isInitialized)
        {
            UpdateMovementPause();
            if (CanProcess) ResolveWaitingPassengers();
            else RefreshPassengerMovement();
        }
    }

    private void OnDisable()
    {
        GameEvents.LevelLoaded -= HandleLevelLoaded;
        GameEvents.LevelStateChanged -= HandleLevelStateChanged;
        GameEvents.ActiveBusChanged -= ResolveWaitingPassengers;

        UpdateMovementPause();
        RefreshPassengerMovement();
    }

    private void Update()
    {
        if (!CanProcess || _passengerSpawner == null || _passengerSpawner.Passengers == null) return;

        float dt = Time.deltaTime;
        IReadOnlyList<Passenger> passengers = _passengerSpawner.Passengers;
        for (int i = 0; i < passengers.Count; i++)
        {
            Passenger passenger = passengers[i];
            if (passenger != null)
            {
                passenger.TickMechanic(dt);
            }
        }
    }

    private void HandleLevelLoaded(LevelDataSO data, float timeLimit, LevelState state)
    {
        Initialize();
    }

    private void HandleLevelStateChanged(LevelState state)
    {
        SetInputEnabled(state == LevelState.Playing);
        if (state is LevelState.Loading or LevelState.None) CancelMovements();
    }

    public void Initialize()
    {
        Clear();

        _inputEnabled = false;
        _gridDirty = true;
        _remainingPassengers = 0;

        if (_passengerSpawner != null && _passengerSpawner.Passengers != null)
        {
            foreach (Passenger passenger in _passengerSpawner.Passengers)
            {
                if (passenger != null && passenger.State != PassengerState.Boarded) _remainingPassengers++;
            }
        }

        Transform walkway = _waitingSlots != null ? _waitingSlots.transform : null;
        _boardingCoordinator.Initialize(_gridBuilder, _waitingSlots, _busQueue, walkway);
        _boardingCoordinator.PassengerBoarded += HandlePassengerBoarded;
        _boardingCoordinator.Failed += Fail;
        _boardingCoordinator.TripsChanged += HandleTripsChanged;

        _isInitialized = true;

        if (_passengerSpawner != null && _passengerSpawner.Passengers != null)
        {
            foreach (Passenger passenger in _passengerSpawner.Passengers)
            {
                if (passenger == null) continue;
                passenger.Clicked += HandlePassengerClicked;
                if (passenger.Movement != null)
                    passenger.Movement.MovementCancelled += HandleMovementCancelled;
            }
        }

        RefreshPassengerMovement();
    }

    private void SetInputEnabled(bool isEnabled)
    {
        _inputEnabled = isEnabled;
        UpdateMovementPause();

        if (CanProcess) ResolveWaitingPassengers();
        else RefreshPassengerMovement();
    }

    private void UpdateMovementPause()
    {
        _boardingCoordinator.UpdateMovementPause(!CanProcess);
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

        if (_waitingSlots == null)
        {
            Fail("PassengerController requires a waiting slots reference.");
            return;
        }

        UpdateReachabilityIfNeeded();

        if (!_gridPathfinder.TryGetPath(passenger, _gridBuilder, out List<Vector3> route)) return;

        Bus bus = _busQueue != null ? _busQueue.ActiveBus : null;
        bool movedFromGrid = false;

        if (_boardingCoordinator.CanReserveBus(passenger, bus))
        {
            if (!bus.HasBoardingPoint)
            {
                Fail("Bus requires a boarding point.");
                return;
            }

            _gridDirty = true;
            movedFromGrid = true;
            _boardingCoordinator.StartTripToBus(passenger, bus, route);

            GameEvents.TriggerPassengerMoveStarted();
        }
        else if (_waitingSlots != null && _waitingSlots.TryReserveSlot(passenger, out GridCell slot))
        {
            _gridDirty = true;
            movedFromGrid = true;
            _boardingCoordinator.StartTripToSlot(passenger, slot, route);

            GameEvents.TriggerPassengerMoveStarted();
        }

        if (movedFromGrid)
        {
            NotifyOtherPassengersMoved(passenger);
        }

        RefreshPassengerMovement();
        CheckResult();
    }

    private void NotifyOtherPassengersMoved(Passenger movedPassenger)
    {
        if (_passengerSpawner == null || _passengerSpawner.Passengers == null) return;

        IReadOnlyList<Passenger> passengers = _passengerSpawner.Passengers;
        for (int i = 0; i < passengers.Count; i++)
        {
            Passenger other = passengers[i];
            if (other != null && other != movedPassenger)
            {
                other.NotifyOtherPassengerMoved(movedPassenger);
            }
        }
    }

    private void HandlePassengerBoarded()
    {
        _remainingPassengers--;
    }

    private void HandleTripsChanged()
    {
        RefreshPassengerMovement();
        CheckResult();
        ResolveWaitingPassengers();
    }

    private void ResolveWaitingPassengers()
    {
        if (!CanProcess) return;

        _boardingCoordinator.ResolveWaitingPassengers(() => CanProcess);
        RefreshPassengerMovement();
        CheckResult();
    }

    private void RefreshPassengerMovement()
    {
        if (!_isInitialized) return;

        bool canProcess = CanProcess;
        if (canProcess) UpdateReachabilityIfNeeded();

        if (_passengerSpawner != null && _passengerSpawner.Passengers != null)
        {
            foreach (Passenger passenger in _passengerSpawner.Passengers)
            {
                if (passenger == null) continue;

                if (canProcess)
                {
                    bool canReach = _gridPathfinder.CanReachExit(passenger, _gridBuilder);
                    passenger.UpdateReachability(canReach);
                }
                else
                {
                    passenger.SetCanMove(false);
                }
            }
        }
    }

    private void CheckResult()
    {
        if (!CanProcess) return;

        if (_busQueue != null && _busQueue.IsTransitioning) return;

        if (_waitingSlots != null && _waitingSlots.AllPassengersArrived)
        {
            Fail("All waiting slots are occupied.");
            return;
        }

        if (_boardingCoordinator.ActiveTripCount > 0) return;

        if (_remainingPassengers == 0 && (_busQueue == null || _busQueue.IsEmpty) && (_waitingSlots == null || _waitingSlots.IsEmpty))
        {
            GameEvents.TriggerLevelCompleted();
        }
    }

    private void Fail(string reason)
    {
        if (!_isInitialized || !_inputEnabled) return;

        GameEvents.TriggerLevelFailed(reason);
    }

    private void HandleMovementCancelled(PassengerMovement movement)
    {
        if (!_isInitialized) return;

        Passenger passenger = movement != null ? movement.GetComponent<Passenger>() : null;
        if (passenger != null && _boardingCoordinator.HasActiveTripFor(passenger))
        {
            Fail("A passenger movement was interrupted.");
        }
    }

    public void CancelMovements() => _boardingCoordinator.CancelMovements();

    public void Clear()
    {
        _inputEnabled = false;

        _boardingCoordinator.PassengerBoarded -= HandlePassengerBoarded;
        _boardingCoordinator.Failed -= Fail;
        _boardingCoordinator.TripsChanged -= HandleTripsChanged;
        _boardingCoordinator.Clear();

        if (_passengerSpawner != null && _passengerSpawner.Passengers != null)
        {
            foreach (Passenger passenger in _passengerSpawner.Passengers)
            {
                if (passenger == null) continue;
                passenger.Clicked -= HandlePassengerClicked;
                passenger.SetCanMove(false);

                if (passenger.Movement != null)
                    passenger.Movement.MovementCancelled -= HandleMovementCancelled;
            }
        }

        _gridPathfinder.Clear();
        _gridDirty = true;
        _isInitialized = false;
        _remainingPassengers = 0;
    }
}
