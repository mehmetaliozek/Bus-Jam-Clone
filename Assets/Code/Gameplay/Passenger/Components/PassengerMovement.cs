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
    private bool _paused;
    private bool _usesRunAnimation;

    private bool IsMoving => _tween != null && _tween.IsActive();

    public void Move(IReadOnlyList<Vector3> route, Action onArrived)
    {
        if (route == null || route.Count == 0) throw new ArgumentException("Movement requires a route.");

        Cancel();
        _usesRunAnimation = true;
        _onArrived = onArrived;
        _previousPosition = transform.position;
        _paused = false;

        Vector3[] points = new Vector3[route.Count];
        float distance = 0f;
        Vector3 prev = transform.position;

        for (int i = 0; i < route.Count; i++)
        {
            points[i] = route[i];
            distance += Vector3.Distance(prev, points[i]);
            prev = points[i];
        }

        float duration = Mathf.Max(0.02f, distance / Mathf.Max(0.1f, _speed));

        _tween = transform
            .DOPath(points, duration, PathType.Linear)
            .SetEase(Ease.Linear)
            .OnUpdate(UpdateRotation)
            .OnComplete(CompleteMovement);

        RunningChanged?.Invoke(true);
    }

    private void UpdateRotation()
    {
        Vector3 direction = transform.position - _previousPosition;
        _previousPosition = transform.position;

        // Y eksenini sıfırlayarak yolcunun öne/arkaya/yana eğilmesini engeller, dimdik tutar.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.000001f) return;

        Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _rotationSpeed * Time.deltaTime);
    }

    public void MoveToSeat(Transform seat, Action onArrived)
    {
        if (seat == null) throw new ArgumentNullException(nameof(seat));

        Cancel();
        _usesRunAnimation = false;
        _onArrived = onArrived;
        _paused = false;

        float duration = Mathf.Max(0.05f, _seatMoveDuration);

        _tween = DOTween.Sequence()
            .Append(transform.DOJump(seat.position, _seatJumpHeight, 1, duration))
            .Join(transform.DORotateQuaternion(seat.rotation, duration).SetEase(Ease.OutQuad))
            .OnComplete(CompleteMovement);

        RunningChanged?.Invoke(false);
    }

    private void CompleteMovement()
    {
        Action callback = _onArrived;
        _tween = null;
        _onArrived = null;
        _paused = false;

        RunningChanged?.Invoke(false);
        callback?.Invoke();
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
        if (_tween == null) return;

        Tween t = _tween;
        _tween = null;
        _onArrived = null;
        _paused = false;

        t.Kill(false);
        RunningChanged?.Invoke(false);
        MovementCancelled?.Invoke(this);
    }

    private void OnDisable() => Cancel();
    private void OnDestroy() => Cancel();
}
