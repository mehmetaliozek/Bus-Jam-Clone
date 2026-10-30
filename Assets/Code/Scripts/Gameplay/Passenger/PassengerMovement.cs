using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PassengerMovement : MonoBehaviour
{
    public event Action<PassengerMovement> MovementCancelled;
    public event Action<bool> RunningChanged;

    [SerializeField, Min(0.1f)] private float _speed = 4f;
    [SerializeField, Min(1f)] private float _rotationSpeed = 720f;
    [SerializeField, Min(0.05f)] private float _seatMoveDuration = 0.35f;
    [SerializeField, Min(0f)] private float _seatJumpHeight = 0.5f;

    private Tween _tween;
    private Action _onArrived;
    private Vector3 _previousPosition;

    private bool _usesRunAnimation;
    private bool _paused;

    public bool IsMoving => _tween != null && _tween.IsActive();


    public void Move(IReadOnlyList<Vector3> route, Action onArrived)
    {
        if (route == null || route.Count == 0) throw new ArgumentException("Movement requires a route.");

        Cancel();
        _usesRunAnimation = true;

        Vector3[] points = new Vector3[route.Count];
        Vector3 previous = transform.position;
        float distance = 0f;

        for (int i = 0; i < route.Count; i++)
        {
            points[i] = route[i];
            distance += Vector3.Distance(previous, points[i]);
            previous = points[i];
        }

        _onArrived = onArrived;
        _previousPosition = transform.position;
        _paused = false;

        float duration = Mathf.Max(0.02f, distance / Mathf.Max(0.1f, _speed));

        _tween = transform
            .DOPath(points, duration, PathType.Linear)
            .SetEase(Ease.Linear)
            .OnUpdate(UpdateRotation)
            .OnComplete(CompleteMovement);

        RunningChanged?.Invoke(true);
    }

    public void MoveToSeat(Transform seat, Action onArrived)
    {
        if (seat == null) throw new ArgumentNullException(nameof(seat));

        Cancel();

        _usesRunAnimation = false;
        _onArrived = onArrived;
        _paused = false;

        float duration = Mathf.Max(0.05f, _seatMoveDuration);

        Sequence sequence = DOTween.Sequence();
        _tween = sequence;

        sequence.Append(transform.DOJump(seat.position, _seatJumpHeight, 1, duration));

        sequence.Join(transform.DORotateQuaternion(seat.rotation, duration).SetEase(Ease.OutQuad));

        sequence.OnComplete(CompleteMovement);

        RunningChanged?.Invoke(false);
    }
    private void UpdateRotation()
    {
        Vector3 direction = transform.position - _previousPosition;
        _previousPosition = transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.000001f) return;

        Quaternion target = Quaternion.LookRotation(direction, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _rotationSpeed * Time.deltaTime);
    }

    private void CompleteMovement()
    {
        Action onArrived = _onArrived;

        _tween = null;
        _onArrived = null;
        _paused = false;

        RunningChanged?.Invoke(false);

        onArrived?.Invoke();
    }

    public void SetPaused(bool paused)
    {
        if (!IsMoving || _paused == paused) return;

        _paused = paused;

        if (paused)
        {
            _tween.Pause();
        }
        else
        {
            _previousPosition = transform.position;
            _tween.Play();
        }

        RunningChanged?.Invoke(!paused && _usesRunAnimation);
    }

    public void Cancel()
    {
        bool hadMovement = _tween != null;

        Tween tween = _tween;

        _tween = null;
        _onArrived = null;
        _paused = false;

        tween?.Kill(false);

        if (!hadMovement) return;

        RunningChanged?.Invoke(false);
        MovementCancelled?.Invoke(this);
    }

    private void OnDisable()
    {
        Cancel();
    }

    private void OnDestroy()
    {
        Cancel();
    }
}