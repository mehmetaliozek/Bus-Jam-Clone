using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BusQueue : MonoBehaviour
{

    public event Action ActiveBusChanged;

    [SerializeField] private Bus _busPrefab;
    [SerializeField] private Transform _exitPoint;
    [SerializeField] private Vector3 _queueOffset = new(-8f, 0f, 0f);

    [SerializeField, Min(0f)] private float _departureDelay = 0.2f;
    [SerializeField, Min(0.1f)] private float _departureDuration = 0.9f;
    [SerializeField, Min(0.1f)] private float _arrivalDuration = 0.8f;

    private readonly Queue<Bus> _busQueue = new();

    private PrefabPool<Bus> _pool;
    private Sequence _transition;

    private bool _isTransitioning;
    private bool _paused = true;
    private int _version;

    public Bus ActiveBus => !_isTransitioning && _busQueue.Count > 0 ? _busQueue.Peek() : null;

    public bool IsBoardingOpen => isActiveAndEnabled && !_paused && !_isTransitioning && ActiveBus != null;

    public bool IsEmpty => _busQueue.Count == 0 && !_isTransitioning;

    public bool IsConfigured => _busPrefab != null && _busPrefab.IsConfigured && _exitPoint != null && _queueOffset.sqrMagnitude > 0f;

    public bool IsTransitioning => _isTransitioning;


    private void Awake()
    {
        if (_busPrefab != null) EnsurePool();
    }

    private void EnsurePool()
    {
        if (_pool != null) return;

        _pool = new PrefabPool<Bus>(
            _busPrefab,
            transform,
            initialCount: 10,
            maxSize: 34
        );
    }

    private void ReleaseBus(Bus bus)
    {
        if (bus == null) return;

        bus.PrepareForPool();
        _pool.Release(bus);
    }

    public void Build(IReadOnlyList<BusData> buses)
    {
        EnsurePool();

        for (int i = 0; i < buses.Count; i++)
        {
            Vector3 position = transform.TransformPoint(_queueOffset * i);

            Bus bus = _pool.Get(
                transform,
                position,
                transform.rotation
            );

            bus.Initialize(buses[i]);
            _busQueue.Enqueue(bus);
        }

        ActiveBusChanged?.Invoke();
    }

    public void AdvanceIfFull()
    {
        if (_isTransitioning) return;

        Bus departingBus = ActiveBus;

        if (departingBus == null || !departingBus.IsFull) return;

        _isTransitioning = true;

        int version = _version;

        Sequence sequence = DOTween.Sequence();
        _transition = sequence;

        sequence.AppendInterval(_departureDelay);

        sequence.Append(departingBus.transform.DOMove(_exitPoint.position, Mathf.Max(0.1f, _departureDuration)).SetEase(Ease.InQuad));

        sequence.OnComplete(() =>
        {
            if (version != _version) return;

            _transition = null;

            _busQueue.Dequeue();

            ReleaseBus(departingBus);

            MoveQueueForward(version);
        });

        ApplyPause();
    }

    private void MoveQueueForward(int version)
    {
        if (version != _version) return;

        if (_busQueue.Count == 0)
        {
            FinishTransition();
            return;
        }

        Sequence sequence = DOTween.Sequence();
        _transition = sequence;

        int index = 0;

        foreach (Bus bus in _busQueue)
        {
            Vector3 destination = transform.TransformPoint(_queueOffset * index);

            sequence.Insert(0f, bus.transform.DOMove(destination, Mathf.Max(0.1f, _arrivalDuration)).SetEase(Ease.InOutSine));

            index++;
        }

        sequence.OnComplete(() =>
        {
            if (version != _version) return;

            FinishTransition();
        });

        ApplyPause();
    }

    private void FinishTransition()
    {
        _transition = null;
        _isTransitioning = false;

        ActiveBusChanged?.Invoke();
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        ApplyPause();
    }

    private void ApplyPause()
    {
        if (_transition == null || !_transition.IsActive()) return;

        if (_paused || !isActiveAndEnabled)
        {
            _transition.Pause();
        }
        else
        {
            _transition.Play();
        }
    }

    public void StopTransition()
    {
        _version++;
        _paused = true;

        Sequence transition = _transition;
        _transition = null;

        transition?.Kill(false);
    }

    public void Clear()
    {
        StopTransition();

        while (_busQueue.Count > 0)
        {
            ReleaseBus(_busQueue.Dequeue());
        }

        _isTransitioning = false;
    }

    private void OnEnable()
    {
        ApplyPause();
    }

    private void OnDisable()
    {
        ApplyPause();
    }

    private void OnDestroy()
    {
        Clear();
        _pool?.Dispose();
    }
}