using System;
using UnityEngine;

[RequireComponent(typeof(PassengerMovement))]
[RequireComponent(typeof(PassengerAnimatorController))]
public class Passenger : MonoBehaviour
{
    public event Action<Passenger> Clicked;

    [SerializeField] private Renderer _renderer;

    public GameColor Color { get; private set; }
    public Vector2Int Coordinates { get; private set; }
    public PassengerState State { get; private set; }
    public bool CanMove { get; private set; }
    public PassengerMovement Movement { get; private set; }

    private PassengerAnimatorController _animatorController;
    private Transform _spawnParent;


    private void Awake()
    {
        Movement = GetComponent<PassengerMovement>();
        _animatorController = GetComponent<PassengerAnimatorController>();
    }

    public void Initialize(PassengerData data)
    {
        _spawnParent = transform.parent;

        Color = data.Color;
        Coordinates = data.Coordinates;
        State = PassengerState.OnGrid;
        CanMove = false;

        _animatorController.SetRunning(false);
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
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
        _animatorController.SetSitting(true);
    }

    public void HideAfterDeparture()
    {
        gameObject.SetActive(false);
        transform.SetParent(_spawnParent, false);
    }
}