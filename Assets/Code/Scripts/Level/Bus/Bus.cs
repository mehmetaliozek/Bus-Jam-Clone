using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Bus : MonoBehaviour
{
    public GameColor Color { get; private set; }
    public int PassengerCount { get; private set; }

    public bool IsFull => PassengerCount == GameConstants.BusCapacity;

    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void Initialize(BusData data)
    {
        Color = data.Color;
        PassengerCount = 0;

        ApplyColor();
    }

    private void ApplyColor()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();

        if (_renderer != null) _renderer.material.color = Color.ToUnityColor();
    }

    public bool TryAddPassenger(Passenger passenger)
    {
        if (passenger == null) return false;
        if (passenger.State == PassengerState.Boarded) return false;
        if (IsFull) return false;
        if (passenger.Color != Color) return false;

        PassengerCount++;
        return true;
    }
}