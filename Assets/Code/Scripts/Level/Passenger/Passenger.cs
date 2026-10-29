using System;
using UnityEngine;
using UnityEngine.UIElements;

public class Passenger : MonoBehaviour
{
    public event Action<Passenger> Clicked;

    public GameColor Color { get; private set; }
    public Vector2Int Coordinates { get; private set; }
    public PassengerState State { get; private set; }
    public bool CanMove { get; private set; }

    public PassengerMovement Movement { get; private set; }

    [SerializeField] private Renderer _renderer;
    private PassengerAnimatorController _animatorController;

    private void Awake()
    {
        Movement = GetComponent<PassengerMovement>();
        _animatorController = GetComponent<PassengerAnimatorController>();
    }

    public void Initialize(PassengerData data)
    {
        Color = data.Color;
        Coordinates = data.Coordinates;
        State = PassengerState.OnGrid;
        CanMove = false;

        _animatorController.SetSitting(false);

        if (_renderer == null) return;

        _renderer.material.color = Color.ToUnityColor();
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

    public void BeginMoving()
    {
        State = PassengerState.Moving;
        CanMove = false;
    }

    public void ArriveAtWaitingSlot()
    {
        State = PassengerState.Waiting;
        CanMove = false;
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    public void Board(Transform seat)
    {
        State = PassengerState.Boarded;
        CanMove = false;

        transform.SetParent(seat, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        _animatorController.SetSitting(true);
    }
}