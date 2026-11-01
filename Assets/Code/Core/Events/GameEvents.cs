using System;

public static class GameEvents
{
    // Level Lifecycle Events
    public static event Action<LevelDataSO, float, LevelState> LevelLoaded;
    public static event Action<LevelState> LevelStateChanged;
    public static event Action<int> LevelNumberChanged;
    public static event Action LevelCompleted;
    public static event Action<string> LevelFailed;

    // Timer Events
    public static event Action<float> TimerChanged;
    public static event Action TimeExpired;

    // Gameplay Events
    public static event Action PassengerMoveStarted;
    public static event Action PassengerBoarded;
    public static event Action ActiveBusChanged;
    public static event Action BusDepartureStarted;

    // Settings State Events
    public static event Action<bool> SoundEnabledChanged;
    public static event Action<bool> VibrationEnabledChanged;

    // UI Request Events
    public static event Action RequestStartLevel;
    public static event Action RequestPauseLevel;
    public static event Action RequestRestartLevel;
    public static event Action RequestNextLevel;
    public static event Action RequestToggleSound;
    public static event Action RequestToggleVibration;
    public static event Action ButtonClicked;

    // Triggers - Level Lifecycle
    public static void TriggerLevelLoaded(LevelDataSO level, float timeLimit, LevelState state) => LevelLoaded?.Invoke(level, timeLimit, state);
    public static void TriggerLevelStateChanged(LevelState state) => LevelStateChanged?.Invoke(state);
    public static void TriggerLevelNumberChanged(int levelNumber) => LevelNumberChanged?.Invoke(levelNumber);
    public static void TriggerLevelCompleted() => LevelCompleted?.Invoke();
    public static void TriggerLevelFailed(string reason) => LevelFailed?.Invoke(reason);

    // Triggers - Timer
    public static void TriggerTimerChanged(float remainingTime) => TimerChanged?.Invoke(remainingTime);
    public static void TriggerTimeExpired() => TimeExpired?.Invoke();

    // Triggers - Gameplay
    public static void TriggerPassengerMoveStarted() => PassengerMoveStarted?.Invoke();
    public static void TriggerPassengerBoarded() => PassengerBoarded?.Invoke();
    public static void TriggerActiveBusChanged() => ActiveBusChanged?.Invoke();
    public static void TriggerBusDepartureStarted() => BusDepartureStarted?.Invoke();

    // Triggers - Settings State
    public static void TriggerSoundEnabledChanged(bool isEnabled) => SoundEnabledChanged?.Invoke(isEnabled);
    public static void TriggerVibrationEnabledChanged(bool isEnabled) => VibrationEnabledChanged?.Invoke(isEnabled);

    // Triggers - UI Requests
    public static void TriggerRequestStartLevel() => RequestStartLevel?.Invoke();
    public static void TriggerRequestPauseLevel() => RequestPauseLevel?.Invoke();
    public static void TriggerRequestRestartLevel() => RequestRestartLevel?.Invoke();
    public static void TriggerRequestNextLevel() => RequestNextLevel?.Invoke();
    public static void TriggerRequestToggleSound() => RequestToggleSound?.Invoke();
    public static void TriggerRequestToggleVibration() => RequestToggleVibration?.Invoke();
    public static void TriggerButtonClicked() => ButtonClicked?.Invoke();
}
