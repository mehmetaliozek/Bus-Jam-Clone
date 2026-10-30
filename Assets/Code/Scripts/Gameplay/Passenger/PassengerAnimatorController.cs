using UnityEngine;

[RequireComponent(typeof(PassengerMovement))]
public class PassengerAnimatorController : MonoBehaviour
{
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int IsSitting = Animator.StringToHash("IsSitting");

    [SerializeField] private Animator _animator;

    private PassengerMovement _movement;

    private void Awake()
    {
        _movement = GetComponent<PassengerMovement>();
    }

    private void OnEnable()
    {
        _movement.RunningChanged += SetRunning;

        SetRunning(false);
    }

    private void OnDisable()
    {
        if (_movement != null) _movement.RunningChanged -= SetRunning;
    }

    public void SetRunning(bool running)
    {
        if (_animator == null) return;

        _animator.SetBool(IsRunning, running);

        if (running)
            _animator.SetBool(IsSitting, false);
    }

    public void SetSitting(bool sitting)
    {
        if (_animator == null) return;

        _animator.SetBool(IsSitting, sitting);

        if (sitting)
            _animator.SetBool(IsRunning, false);
    }
}