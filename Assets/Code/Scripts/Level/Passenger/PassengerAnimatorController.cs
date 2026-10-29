using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PassengerMovement))]
public class PassengerAnimatorController : MonoBehaviour
{
    public event Action<string> AnimationStarted;
    public event Action<string> AnimationEnded;

    [SerializeField] private Animator _animator;

    private static readonly int IsRunning = Animator.StringToHash("IsRunning");

    private static readonly int IsSitting = Animator.StringToHash("IsSitting");

    private readonly HashSet<string> _activeStates = new HashSet<string>();

    private PassengerMovement _movement;

    private void OnEnable()
    {
        _movement = GetComponent<PassengerMovement>();
        _movement.RunningChanged += SetRunning;

        SetRunning(false);
    }

    private void OnDisable()
    {
        if (_movement != null) _movement.RunningChanged -= SetRunning;

        string[] states = new string[_activeStates.Count];
        _activeStates.CopyTo(states);
        _activeStates.Clear();

        foreach (string state in states) AnimationEnded?.Invoke(state);
    }

    public void SetRunning(bool running)
    {
        if (_animator == null) return;

        _animator.SetBool(IsRunning, running);

        if (running) _animator.SetBool(IsSitting, false);
    }

    public void SetSitting(bool sitting)
    {
        if (_animator == null) return;

        _animator.SetBool(IsSitting, sitting);

        if (sitting) _animator.SetBool(IsRunning, false);
    }

    public void NotifyStateEntered(string stateName)
    {
        if (!isActiveAndEnabled) return;

        if (_activeStates.Add(stateName)) AnimationStarted?.Invoke(stateName);
    }

    public void NotifyStateExited(string stateName)
    {
        if (_activeStates.Remove(stateName)) AnimationEnded?.Invoke(stateName);
    }
}