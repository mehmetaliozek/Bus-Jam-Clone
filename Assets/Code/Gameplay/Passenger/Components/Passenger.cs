using System;
using UnityEngine;

[RequireComponent(typeof(PassengerMovement))]
[RequireComponent(typeof(PassengerAnimatorController))]
public class Passenger : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static MaterialPropertyBlock _mpb;

    public event Action<Passenger> Clicked;

    [SerializeField] private Renderer _renderer;
    [SerializeField] private Material _outlineMaterial;

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
        SetState(PassengerState.OnGrid);
        SetCanMove(false);

        _animatorController.SetRunning(false);
        _animatorController.SetSitting(false);

        ApplyColor();
    }

    private void ApplyColor()
    {
        if (_renderer == null) return;

        _mpb ??= new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_mpb, 0);
        Color unityColor = Color.ToUnityColor();
        _mpb.SetColor(BaseColorId, unityColor);
        _mpb.SetColor(ColorId, unityColor);
        _renderer.SetPropertyBlock(_mpb, 0);
    }

    private void OnDisable()
    {
        SetOutline(false);
    }

    private void OnMouseDown()
    {
        NotifyClicked();
    }

    private void NotifyClicked()
    {
        if (!CanMove) return;

        Clicked?.Invoke(this);
    }

    public void SetCanMove(bool canMove)
    {
        bool newCanMove = State == PassengerState.OnGrid && canMove;
        if (CanMove == newCanMove) return;

        CanMove = newCanMove;
        SetOutline(CanMove);
    }

    private void SetOutline(bool show)
    {
        if (_renderer == null || _outlineMaterial == null) return;

        _renderer.sharedMaterials = show
            ? new[] { _renderer.sharedMaterial, _outlineMaterial }
            : new[] { _renderer.sharedMaterial };
    }

    private void SetState(PassengerState newState)
    {
        if (State == newState) return;

        State = newState;
    }

    public void BeginMoving()
    {
        SetState(PassengerState.Moving);
        SetCanMove(false);
    }

    public void ArriveAtWaitingSlot()
    {
        SetState(PassengerState.Waiting);
        SetCanMove(false);
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    public void Board(Transform seat)
    {
        SetState(PassengerState.Boarded);
        SetCanMove(false);

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
