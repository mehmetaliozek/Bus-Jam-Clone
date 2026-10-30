using Solo.MOST_IN_ONE;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VibrationManager : MonoBehaviour
{
    public event Action<bool> VibrationEnabledChanged;

    [Header("References")]
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private PassengerController _passengerController;

    [Header("Haptic Types")]
    [SerializeField]
    private MOST_HapticFeedback.HapticTypes _passengerClickHaptic = MOST_HapticFeedback.HapticTypes.LightImpact;

    [SerializeField]
    private MOST_HapticFeedback.HapticTypes _passengerBoardedHaptic = MOST_HapticFeedback.HapticTypes.MediumImpact;

    private bool _isVibrationEnabled = true;

    public bool IsVibrationEnabled => _isVibrationEnabled;

    private void Awake()
    {
        MOST_HapticFeedback.Prewarm();
        SetVibrationEnabled(_isVibrationEnabled);
    }

    private void OnEnable()
    {
        if (_passengerController != null)
        {
            _passengerController.PassengerMoveStarted += HandlePassengerMoveStarted;

            _passengerController.PassengerBoarded += HandlePassengerBoarded;
        }

        if (_levelManager != null)
        {
            _levelManager.StateChanged += HandleLevelStateChanged;
        }
    }

    private void OnDisable()
    {
        if (_passengerController != null)
        {
            _passengerController.PassengerMoveStarted -= HandlePassengerMoveStarted;

            _passengerController.PassengerBoarded -= HandlePassengerBoarded;
        }

        if (_levelManager != null)
        {
            _levelManager.StateChanged -= HandleLevelStateChanged;
        }

        MOST_HapticFeedback.Stop();
    }

    private void HandlePassengerMoveStarted()
    {
        PlayHaptic(_passengerClickHaptic);
    }

    private void HandlePassengerBoarded()
    {
        PlayHaptic(_passengerBoardedHaptic);
    }

    private void PlayHaptic(MOST_HapticFeedback.HapticTypes type)
    {
        if (!isActiveAndEnabled || !_isVibrationEnabled) return;

        MOST_HapticFeedback.Generate(type);
    }

    private void HandleLevelStateChanged(LevelState state)
    {
        if (state == LevelState.Loading || state == LevelState.Paused || state == LevelState.Failed)
        {
            MOST_HapticFeedback.Stop();
        }
    }

    public void SetVibrationEnabled()
    {
        SetVibrationEnabled(!_isVibrationEnabled);
    }

    public void SetVibrationEnabled(bool isEnabled)
    {
        _isVibrationEnabled = isEnabled;

        if (MOST_HapticFeedback.HapticsEnabled != isEnabled)
        {
            MOST_HapticFeedback.HapticsEnabled = isEnabled;
        }

        if (!isEnabled)
        {
            MOST_HapticFeedback.Stop();
        }

        VibrationEnabledChanged?.Invoke(isEnabled);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            MOST_HapticFeedback.Stop();
        }
    }
}