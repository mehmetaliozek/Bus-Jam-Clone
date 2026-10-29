using System.Collections.Generic;
using UnityEngine;

public class PassengerSpawner : MonoBehaviour
{
    [SerializeField] private Passenger _passengerPrefab;

    private List<Passenger> _passengers = new List<Passenger>();

    public IReadOnlyList<Passenger> Passengers => _passengers;

    public bool IsConfigured => _passengerPrefab != null;

    public void Spawn(IReadOnlyList<PassengerData> passengers, GridBuilder gridBuilder)
    {
        Clear();

        for (int i = 0; i < passengers.Count; i++)
        {
            PassengerData data = passengers[i];
            GridCell cell = gridBuilder.GetCell(data.Coordinates);

            Passenger passenger = Instantiate(_passengerPrefab, cell.PassengerPosition, gridBuilder.transform.rotation, transform);

            passenger.Initialize(data);
            cell.SetPassenger(passenger);

            _passengers.Add(passenger);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _passengers.Count; i++)
        {
            Passenger passenger = _passengers[i];

            if (passenger == null) continue;

            passenger.gameObject.SetActive(false);
            Destroy(passenger.gameObject);
        }

        _passengers.Clear();
    }
}