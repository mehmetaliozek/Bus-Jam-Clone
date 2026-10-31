using System.Collections.Generic;
using UnityEngine;

public class Bus : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock _mpb;

    [SerializeField] private Transform _boardingPoint;
    [SerializeField] private Transform[] _seats;
    [SerializeField] private Renderer _renderer;

    private readonly int[] _colorMaterialIndices = { 0, 1 };
    private readonly Dictionary<Passenger, Transform> _seatReservations = new();
    private readonly HashSet<Passenger> _seatedPassengers = new();

    public GameColor Color { get; private set; }
    public int PassengerCount => _seatedPassengers.Count;
    public bool IsFull => PassengerCount == GameConstants.BusCapacity;
    public bool HasBoardingPoint => _boardingPoint != null;
    public Vector3 BoardingPosition => _boardingPoint.position;

    public bool IsConfigured => _boardingPoint != null && _seats != null && _seats.Length == GameConstants.BusCapacity;

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
        if (passenger == null || !IsConfigured || passenger.Color != Color || passenger.State == PassengerState.Boarded)
            return false;

        if (_seatReservations.TryGetValue(passenger, out seat)) return true;

        foreach (Transform candidate in _seats)
        {
            if (_seatReservations.ContainsValue(candidate)) continue;
            _seatReservations[passenger] = candidate;
            seat = candidate;
            return true;
        }

        return false;
    }

    public bool TryCompleteSeating(Passenger passenger, Transform seat)
    {
        if (passenger == null || seat == null) return false;
        if (!_seatReservations.TryGetValue(passenger, out Transform reserved) || reserved != seat) return false;
        return _seatedPassengers.Add(passenger);
    }

    public void ReleaseSeatReservation(Passenger passenger)
    {
        if (passenger != null && !_seatedPassengers.Contains(passenger))
        {
            _seatReservations.Remove(passenger);
        }
    }

    private void ApplyColor()
    {
        if (_renderer == null) return;

        _mpb ??= new MaterialPropertyBlock();
        Color unityColor = Color.ToUnityColor();
        _mpb.SetColor(ColorId, unityColor);
        _mpb.SetColor(BaseColorId, unityColor);

        foreach (int index in _colorMaterialIndices)
        {
            _renderer.SetPropertyBlock(_mpb, index);
        }
    }

    public void PrepareForPool()
    {
        foreach (Passenger passenger in _seatedPassengers)
        {
            if (passenger != null) passenger.HideAfterDeparture();
        }

        _seatedPassengers.Clear();
        _seatReservations.Clear();
    }
}
