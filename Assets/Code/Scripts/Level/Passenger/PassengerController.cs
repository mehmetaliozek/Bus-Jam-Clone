using System;
using UnityEngine;

public class PassengerController : MonoBehaviour
{
    public event Action LevelCompleted;
    public event Action<string> LevelFailed;

    private GridBuilder _gridBuilder;
    private PassengerSpawner _passengerSpawner;
    private BusQueue _busQueue;
    private WaitingSlots _waitingSlots;

    private GridPathfinder _gridPathfinder = new GridPathfinder();

    private bool _inputEnabled;
    private bool _isProcessing;
    private bool _isInitialized;
    private bool _eventsSubscribed;

    private int _remainingPassengers;

    private bool CanProcess => _isInitialized && isActiveAndEnabled && _inputEnabled && !_isProcessing;

    private void OnEnable()
    {
        if (!_isInitialized) return;

        SubscribeEvents();

        if (_inputEnabled)
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
        UnsubscribeEvents();
    }

    public void Initialize(GridBuilder gridBuilder, PassengerSpawner passengerSpawner, BusQueue busQueue, WaitingSlots waitingSlots)
    {
        Clear();

        _gridBuilder = gridBuilder;
        _passengerSpawner = passengerSpawner;
        _busQueue = busQueue;
        _waitingSlots = waitingSlots;

        _remainingPassengers = _passengerSpawner.Passengers.Count;
        _isInitialized = true;

        if (isActiveAndEnabled)
        {
            SubscribeEvents();
        }

        RefreshPassengerMovement();
    }

    private void SubscribeEvents()
    {
        if (!_isInitialized || _eventsSubscribed) return;

        for (int i = 0; i < _passengerSpawner.Passengers.Count; i++)
        {
            Passenger passenger = _passengerSpawner.Passengers[i];

            if (passenger == null) continue;

            passenger.Clicked += HandlePassengerClicked;
        }

        _busQueue.ActiveBusChanged += HandleActiveBusChanged;

        _eventsSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_eventsSubscribed) return;

        if (_passengerSpawner != null)
        {
            for (int i = 0; i < _passengerSpawner.Passengers.Count; i++)
            {
                Passenger passenger = _passengerSpawner.Passengers[i];

                if (passenger == null) continue;

                passenger.Clicked -= HandlePassengerClicked;
            }
        }

        if (_busQueue != null)
        {
            _busQueue.ActiveBusChanged -= HandleActiveBusChanged;
        }

        _eventsSubscribed = false;
    }

    public void SetInputEnabled(bool isEnabled)
    {
        if (_inputEnabled == isEnabled) return;

        _inputEnabled = isEnabled;

        if (_inputEnabled)
        {
            ResolveWaitingPassengers();
        }
    }

    private void HandlePassengerClicked(Passenger passenger)
    {
        if (!CanProcess) return;
        if (!CanSelectPassenger(passenger)) return;

        bool passengerMoved = false;

        _isProcessing = true;

        try
        {
            passengerMoved = TryBoardPassenger(passenger);

            if (!passengerMoved && _waitingSlots.TryAddPassengerToSlot(passenger))
            {
                ReleaseGridCell(passenger);
                passengerMoved = true;
            }
        }
        finally
        {
            _isProcessing = false;
        }

        if (passengerMoved)
        {
            ResolveWaitingPassengers();
        }
        else
        {
            CheckResult();
        }
    }

    private bool CanSelectPassenger(Passenger passenger)
    {
        if (passenger == null) return false;
        if (passenger.State != PassengerState.OnGrid) return false;

        return passenger.CanMove;
    }

    private bool TryBoardPassenger(Passenger passenger)
    {
        if (!_busQueue.TryAddPassengerToBus(passenger)) return false;

        if (passenger.State == PassengerState.OnGrid)
        {
            ReleaseGridCell(passenger);
        }
        else if (passenger.State == PassengerState.Waiting)
        {
            _waitingSlots.RemovePassengerFromSlot(passenger);
        }

        passenger.Board();
        _remainingPassengers--;

        _busQueue.AdvanceIfFull();

        return true;
    }

    private void ReleaseGridCell(Passenger passenger)
    {
        if (!_gridBuilder.TryGetCell(passenger.Coordinates, out GridCell cell))
        {
            return;
        }

        cell.RemovePassenger(passenger);
    }

    private void HandleActiveBusChanged()
    {
        if (!CanProcess) return;

        ResolveWaitingPassengers();
    }

    private void ResolveWaitingPassengers()
    {
        if (!CanProcess) return;

        _isProcessing = true;

        try
        {
            while (_isInitialized && isActiveAndEnabled && _inputEnabled && _busQueue.ActiveBus != null)
            {
                GameColor color = _busQueue.ActiveBus.Color;

                if (!_waitingSlots.TryGetPassenger(color, out Passenger passenger))
                {
                    break;
                }

                if (!TryBoardPassenger(passenger))
                {
                    break;
                }
            }
        }
        finally
        {
            _isProcessing = false;
        }

        if (!CanProcess) return;

        RefreshPassengerMovement();
        CheckResult();
    }

    private void RefreshPassengerMovement()
    {
        if (!_isInitialized) return;

        _gridPathfinder.CalculateReachableCells(_gridBuilder);

        for (int i = 0; i < _passengerSpawner.Passengers.Count; i++)
        {
            Passenger passenger = _passengerSpawner.Passengers[i];

            if (passenger == null) continue;

            bool canMove = _gridPathfinder.CanReachExit(passenger, _gridBuilder);

            passenger.SetCanMove(canMove);
        }
    }

    private void CheckResult()
    {
        if (!CanProcess) return;

        if (_remainingPassengers == 0 && _busQueue.IsEmpty && _waitingSlots.IsEmpty)
        {
            _inputEnabled = false;
            LevelCompleted?.Invoke();
            return;
        }

        if (_waitingSlots.IsFull)
        {
            _inputEnabled = false;
            LevelFailed?.Invoke("No waiting slots available.");
        }
    }

    public void Clear()
    {
        UnsubscribeEvents();

        _inputEnabled = false;
        _isProcessing = false;
        _isInitialized = false;
        _remainingPassengers = 0;

        _gridPathfinder.Clear();

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