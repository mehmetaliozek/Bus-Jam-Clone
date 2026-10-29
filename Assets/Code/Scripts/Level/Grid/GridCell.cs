using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Vector2Int Coordinates { get; private set; }
    public Passenger Passenger { get; private set; }

    public bool IsOccupied => Passenger != null;

    [SerializeField] private float _passengerHeight = 1f;

    public Vector3 PassengerPosition => transform.position + transform.up * _passengerHeight;

    public void Initialize(GridCellData data)
    {
        Coordinates = data.Coordinates;
        Passenger = null;
    }

    public void SetPassenger(Passenger passenger)
    {
        Passenger = passenger;
    }

    public void RemovePassenger(Passenger passenger)
    {
        if (Passenger != passenger) return;

        Passenger = null;
    }
}