using System;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Passenger : MonoBehaviour
{
    public event Action<Passenger> Clicked;

    public GameColor Color { get; private set; }
    public Vector2Int Coordinates { get; private set; }
    public PassengerState State { get; private set; }
    public bool CanMove { get; private set; }

    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void Initialize(PassengerData data)
    {
        Color = data.Color;
        Coordinates = data.Coordinates;
        State = PassengerState.OnGrid;
        CanMove = false;

        ApplyColor();
    }

    private void ApplyColor()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();

        if (_renderer != null) _renderer.material.color = Color.ToUnityColor();
    }

    private void OnMouseDown()
    {
        if (!CanMove) return;

        Clicked?.Invoke(this);
    }

    public void SetCanMove(bool canMove)
    {
        CanMove = State == PassengerState.OnGrid && canMove;
    }

    public void MoveToWaitingSlot(Vector3 position)
    {
        State = PassengerState.Waiting;
        CanMove = false;

        transform.position = position;
    }

    public void Board()
    {
        State = PassengerState.Boarded;
        CanMove = false;

        gameObject.SetActive(false);
    }
}