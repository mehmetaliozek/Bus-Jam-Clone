using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GridBuilder _gridBuilder;
    [SerializeField] private PassengerSpawner _passengerSpawner;
    [SerializeField] private BusQueue _busQueue;
    [SerializeField] private WaitingSlots _waitingSlots;

    private LevelData[] _levels = Array.Empty<LevelData>();
    private LevelData _levelData;

    private int _currentLevelIndex = -1;
    private bool _hasLoadedLevel;
    private bool _isSoundEnabled = true;
    private bool _isVibrationEnabled = true;

    public LevelState State { get; private set; } = LevelState.None;
    private int CurrentLevelNumber => _levelData != null ? _levelData.LevelNumber : 0;
    private bool HasNextLevel => _currentLevelIndex >= 0 && _currentLevelIndex + 1 < _levels.Length;

    private void OnEnable()
    {
        GameEvents.LevelCompleted += CompleteLevel;
        GameEvents.LevelFailed += FailLevel;
        GameEvents.TimeExpired += HandleTimeExpired;
        GameEvents.RequestStartLevel += StartLevel;
        GameEvents.RequestPauseLevel += PauseLevel;
        GameEvents.RequestRestartLevel += RestartLevel;
        GameEvents.RequestNextLevel += NextLevel;
        GameEvents.SoundEnabledChanged += HandleSoundEnabledChanged;
        GameEvents.VibrationEnabledChanged += HandleVibrationEnabledChanged;
    }

    private void OnDisable()
    {
        GameEvents.LevelCompleted -= CompleteLevel;
        GameEvents.LevelFailed -= FailLevel;
        GameEvents.TimeExpired -= HandleTimeExpired;
        GameEvents.RequestStartLevel -= StartLevel;
        GameEvents.RequestPauseLevel -= PauseLevel;
        GameEvents.RequestRestartLevel -= RestartLevel;
        GameEvents.RequestNextLevel -= NextLevel;
        GameEvents.SoundEnabledChanged -= HandleSoundEnabledChanged;
        GameEvents.VibrationEnabledChanged -= HandleVibrationEnabledChanged;
    }

    private void HandleSoundEnabledChanged(bool isEnabled) => _isSoundEnabled = isEnabled;
    private void HandleVibrationEnabledChanged(bool isEnabled) => _isVibrationEnabled = isEnabled;

    private bool TryLoadAllLevels(out string error)
    {
        error = null;
        _levels = Resources.LoadAll<LevelData>("Levels");

        if (_levels == null || _levels.Length == 0)
        {
            error = "LevelData could not be found in Resources/Levels.";
            return false;
        }

        Array.Sort(_levels, (first, second) => first.LevelNumber.CompareTo(second.LevelNumber));
        return true;
    }

    private void Start()
    {
        if (!TryLoadAllLevels(out string error))
        {
            FailLevel(error);
            return;
        }

        int startLevel = 0;
        if (SaveManager.TryLoad(out SaveData save))
        {
            _isSoundEnabled = save.IsSoundEnabled;
            _isVibrationEnabled = save.IsVibrationEnabled;

            int index = Array.FindIndex(_levels, l => l.LevelNumber == save.LevelNumber);
            if (index >= 0) startLevel = index;
        }

        GameEvents.TriggerSoundEnabledChanged(_isSoundEnabled);
        GameEvents.TriggerVibrationEnabledChanged(_isVibrationEnabled);

        LoadLevel(startLevel);
    }

    private bool LoadLevel(int index)
    {
        if (index < 0 || index >= _levels.Length)
        {
            Debug.LogError($"Invalid level index: {index}");
            return false;
        }

        _hasLoadedLevel = false;
        LevelData data = _levels[index];
        SetState(LevelState.Loading);

        if (_gridBuilder == null || _passengerSpawner == null || _busQueue == null)
        {
            FailLevel("Level building components are missing.");
            return false;
        }

        ClearLevel();

        _gridBuilder.Build(data.GridSize, data.GridCells);
        _passengerSpawner.Spawn(data.Passengers, _gridBuilder);
        _busQueue.Build(data.Buses);

        _levelData = data;
        _currentLevelIndex = index;
        _hasLoadedLevel = true;

        GameEvents.TriggerLevelNumberChanged(CurrentLevelNumber);
        GameEvents.TriggerLevelLoaded(data, data.TimeLimit, State);
        SetState(LevelState.Ready);
        SaveCurrentGame();

        return true;
    }

    private void ClearLevel()
    {
        if (_busQueue != null) _busQueue.Clear();
        if (_waitingSlots != null) _waitingSlots.Clear();
        if (_passengerSpawner != null) _passengerSpawner.Clear();
        if (_gridBuilder != null) _gridBuilder.Clear();
    }

    private void StartLevel()
    {
        if (State is not (LevelState.Ready or LevelState.Paused)) return;
        SetState(LevelState.Playing);
    }

    private void PauseLevel()
    {
        if (State != LevelState.Playing) return;
        SetState(LevelState.Paused);
    }

    private void RestartLevel()
    {
        if (_currentLevelIndex >= 0) LoadLevel(_currentLevelIndex);
    }

    private void NextLevel()
    {
        if (State == LevelState.Completed && HasNextLevel) LoadLevel(_currentLevelIndex + 1);
    }

    private void CompleteLevel()
    {
        if (State != LevelState.Playing) return;
        SetState(LevelState.Completed);
        Debug.Log($"Level {CurrentLevelNumber} completed.");
    }

    private void FailLevel(string reason)
    {
        if (State is LevelState.Completed or LevelState.Failed) return;
        SetState(LevelState.Failed);
        Debug.LogWarning(reason);
    }

    private void HandleTimeExpired()
    {
        if (State == LevelState.Playing) FailLevel("Time is up.");
    }

    private void SetState(LevelState state)
    {
        if (State == state) return;
        State = state;
        GameEvents.TriggerLevelStateChanged(State);
    }

    private void SaveCurrentGame()
    {
        if (!_hasLoadedLevel || _levelData == null) return;

        SaveManager.Save(new SaveData
        {
            LevelNumber = CurrentLevelNumber,
            IsSoundEnabled = _isSoundEnabled,
            IsVibrationEnabled = _isVibrationEnabled
        });
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused) return;
        PauseLevel();
        SaveCurrentGame();
    }

    private void OnApplicationQuit()
    {
        PauseLevel();
        SaveCurrentGame();
    }
}
