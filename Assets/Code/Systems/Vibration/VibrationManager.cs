using Solo.MOST_IN_ONE;
using UnityEngine;

[DisallowMultipleComponent]
public class VibrationManager : MonoBehaviour
{
    [Header("Haptic Types")]
    [SerializeField] private MOST_HapticFeedback.HapticTypes _passengerClickHaptic = MOST_HapticFeedback.HapticTypes.LightImpact;
    [SerializeField] private MOST_HapticFeedback.HapticTypes _passengerBoardedHaptic = MOST_HapticFeedback.HapticTypes.MediumImpact;

    private bool _isVibrationEnabled = true;

    private void Awake()
    {
        MOST_HapticFeedback.Prewarm();
    }

    private void OnEnable()
    {
        GameEvents.PassengerMoveStarted += HandlePassengerMoveStarted;
        GameEvents.PassengerBoarded += HandlePassengerBoarded;
        GameEvents.LevelStateChanged += HandleLevelStateChanged;
        GameEvents.RequestToggleVibration += HandleRequestToggleVibration;
        GameEvents.VibrationEnabledChanged += HandleVibrationEnabledChanged;
    }

    private void OnDisable()
    {
        GameEvents.PassengerMoveStarted -= HandlePassengerMoveStarted;
        GameEvents.PassengerBoarded -= HandlePassengerBoarded;
        GameEvents.LevelStateChanged -= HandleLevelStateChanged;
        GameEvents.RequestToggleVibration -= HandleRequestToggleVibration;
        GameEvents.VibrationEnabledChanged -= HandleVibrationEnabledChanged;
        MOST_HapticFeedback.Stop();
    }

    private void HandlePassengerMoveStarted() => PlayHaptic(_passengerClickHaptic);
    private void HandlePassengerBoarded() => PlayHaptic(_passengerBoardedHaptic);

    private void HandleRequestToggleVibration()
    {
        GameEvents.TriggerVibrationEnabledChanged(!_isVibrationEnabled);
    }

    private void HandleVibrationEnabledChanged(bool isEnabled)
    {
        if (_isVibrationEnabled == isEnabled && MOST_HapticFeedback.HapticsEnabled == isEnabled) return;
        _isVibrationEnabled = isEnabled;

        if (MOST_HapticFeedback.HapticsEnabled != isEnabled)
        {
            MOST_HapticFeedback.HapticsEnabled = isEnabled;
        }

        if (!isEnabled)
        {
            MOST_HapticFeedback.Stop();
        }
    }

    private void PlayHaptic(MOST_HapticFeedback.HapticTypes type)
    {
        if (isActiveAndEnabled && _isVibrationEnabled) MOST_HapticFeedback.Generate(type);
    }

    private void HandleLevelStateChanged(LevelState state)
    {
        if (state is LevelState.Loading or LevelState.Paused or LevelState.Failed)
        {
            MOST_HapticFeedback.Stop();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) MOST_HapticFeedback.Stop();
    }
}
