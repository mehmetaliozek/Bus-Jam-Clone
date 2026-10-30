using System.Collections.Generic;
using UnityEngine;

public class Bus : MonoBehaviour
{

    [SerializeField] private Transform _boardingPoint;
    [SerializeField] private Transform[] _seats;
    [SerializeField] private Renderer _renderer;

    private readonly int[] _colorMaterialIndices = { 0, 1 };

    private readonly Dictionary<Passenger, Transform> _seatReservations = new();
    private readonly HashSet<Passenger> _seatedPassengers = new();

    private Material[] _materials;

    public GameColor Color { get; private set; }

    public int PassengerCount => _seatedPassengers.Count;
    public bool IsFull => PassengerCount == GameConstants.BusCapacity;

    public bool HasBoardingPoint => _boardingPoint != null;
    public Vector3 BoardingPosition => _boardingPoint.position;


    public bool IsConfigured
    {
        get
        {
            if (_boardingPoint == null) return false;
            if (!_boardingPoint.IsChildOf(transform)) return false;

            if (_seats == null || _seats.Length != GameConstants.BusCapacity)
                return false;

            for (int i = 0; i < _seats.Length; i++)
            {
                if (_seats[i] == null) return false;
                if (!_seats[i].IsChildOf(transform)) return false;

                for (int j = 0; j < i; j++)
                {
                    if (_seats[i] == _seats[j])
                        return false;
                }
            }

            return true;
        }
    }

    public void Initialize(BusData data)
    {
        Color = data.Color;

        _seatReservations.Clear();
        _seatedPassengers.Clear();

        ApplyColor();
    }

    public bool TryReserveSeat(Passenger passenger, out Transform seat)
    {
        seat = null;

        if (passenger == null || !IsConfigured) return false;
        if (passenger.Color != Color) return false;
        if (passenger.State == PassengerState.Boarded) return false;

        if (_seatReservations.TryGetValue(passenger, out seat)) return true;

        foreach (Transform candidate in _seats)
        {
            if (_seatReservations.ContainsValue(candidate)) continue;

            _seatReservations.Add(passenger, candidate);
            seat = candidate;

            return true;
        }

        return false;
    }

    public bool TryCompleteSeating(Passenger passenger, Transform seat)
    {
        if (passenger == null || seat == null) return false;

        if (!_seatReservations.TryGetValue(passenger, out Transform reservedSeat))
            return false;

        if (reservedSeat != seat) return false;

        return _seatedPassengers.Add(passenger);
    }

    public void ReleaseSeatReservation(Passenger passenger)
    {
        if (passenger == null) return;
        if (_seatedPassengers.Contains(passenger)) return;

        _seatReservations.Remove(passenger);
    }

    private void ApplyColor()
    {
        if (_renderer == null) return;

        _materials ??= _renderer.materials;

        foreach (int index in _colorMaterialIndices)
        {
            if (index < 0 || index >= _materials.Length) continue;
            if (_materials[index] == null) continue;

            _materials[index].color = Color.ToUnityColor();
        }
    }

    public void PrepareForPool()
    {
        foreach (Passenger passenger in _seatedPassengers)
        {
            if (passenger != null)
                passenger.HideAfterDeparture();
        }

        _seatedPassengers.Clear();
        _seatReservations.Clear();
    }

    private void OnDestroy()
    {
        if (_materials == null) return;

        foreach (Material material in _materials)
        {
            if (material != null) Destroy(material);
        }
    }
}