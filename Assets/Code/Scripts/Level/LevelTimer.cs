using System;
using UnityEngine;

public class LevelTimer : MonoBehaviour
{
    public event Action<float> TimeChanged;
    public event Action TimeExpired;

    public float RemainingTime { get; private set; }

    private bool _isRunning;

    public void Initialize(float timeLimit)
    {
        _isRunning = false;
        RemainingTime = timeLimit;

        TimeChanged?.Invoke(RemainingTime);
    }

    public void StartTimer()
    {
        if (RemainingTime <= 0f) return;

        _isRunning = true;
    }

    public void StopTimer()
    {
        _isRunning = false;
    }

    private void Update()
    {
        if (!_isRunning) return;

        RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);

        TimeChanged?.Invoke(RemainingTime);

        if (RemainingTime > 0f) return;

        _isRunning = false;
        TimeExpired?.Invoke();
    }
}