using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public event Action<LevelState> StateChanged;

    [SerializeField] private LevelLoader _levelLoader;
    [SerializeField] private PassengerController _passengerController;
    [SerializeField] private LevelTimer _levelTimer;
    [SerializeField] private SoundManager _soundManager;
    [SerializeField] private VibrationManager _vibrationManager;

    private LevelData[] _levels = Array.Empty<LevelData>();
    private LevelData _levelData;

    private int _currentLevelIndex = -1;

    private bool _hasLoadedLevel;

    public LevelState State { get; private set; } = LevelState.None;

    public int CurrentLevelNumber => _levelData != null ? _levelData.LevelNumber : 0;

    public bool HasNextLevel => _currentLevelIndex >= 0 && _currentLevelIndex + 1 < _levels.Length;


    private void OnEnable()
    {
        if (_passengerController != null)
        {
            _passengerController.LevelCompleted += CompleteLevel;
            _passengerController.LevelFailed += FailLevel;
        }

        if (_levelTimer != null)
        {
            _levelTimer.TimeExpired += HandleTimeExpired;
        }

        if (State == LevelState.Playing)
        {
            _passengerController.SetInputEnabled(true);

            if (State == LevelState.Playing)
            {
                _levelTimer.StartTimer();
            }
        }
    }

    private void OnDisable()
    {
        if (_passengerController != null)
        {
            _passengerController.LevelCompleted -= CompleteLevel;
            _passengerController.LevelFailed -= FailLevel;

            _passengerController.SetInputEnabled(false);
        }

        if (_levelTimer != null)
        {
            _levelTimer.TimeExpired -= HandleTimeExpired;
            _levelTimer.StopTimer();
        }
    }

    private void Start()
    {
        if (!TryCollectLevels(out string error))
        {
            FailLevel(error);
            return;
        }

        if (SaveManager.TryLoad(out SaveData save))
        {
            if (_soundManager != null)
            {
                _soundManager.SetSoundEnabled(save.IsSoundEnabled);
            }

            if (_vibrationManager != null)
            {
                _vibrationManager.SetVibrationEnabled(save.IsVibrationEnabled);
            }

            int index = Array.FindIndex(_levels, level => level.LevelNumber == save.LevelNumber);

            if (index >= 0)
            {
                if (save.RestartOnLoad)
                {
                    LoadLevel(index);
                    return;
                }

                if (LoadLevel(index, save)) return;

                Debug.LogWarning("Save could not be applied. Reloading the level.");
                LoadLevel(index);
                return;
            }
        }

        LoadLevel(0);
    }

    private bool TryCollectLevels(out string error)
    {
        error = null;

        _levels = Resources.LoadAll<LevelData>("Levels");

        if (_levels.Length == 0)
        {
            error = "Resources/Levels içinde LevelData bulunamadı.";
            return false;
        }

        Array.Sort(_levels, (first, second) => first.LevelNumber.CompareTo(second.LevelNumber));

        for (int i = 0; i < _levels.Length; i++)
        {
            LevelData level = _levels[i];

            if (level.LevelNumber < 1)
            {
                error = $"{level.name}: LevelNumber pozitif olmalı.";
                return false;
            }

            if (i > 0 && _levels[i - 1].LevelNumber == level.LevelNumber)
            {
                error = $"Aynı LevelNumber birden fazla kullanılmış: {level.LevelNumber}. Asset'ler: {_levels[i - 1].name}, {level.name}";

                return false;
            }
        }

        return true;
    }

    private bool LoadLevel(int index, SaveData save = null)
    {
        if (index < 0 || index >= _levels.Length)
        {
            Debug.LogError($"Invalid level index: {index}");
            return false;
        }

        _hasLoadedLevel = false;

        LevelData data = _levels[index];

        SetState(LevelState.Loading);

        if (_levelLoader == null || _passengerController == null || _levelTimer == null)
        {
            FailLevel("LevelManager references are missing.");
            return false;
        }

        _passengerController.Clear();
        _levelTimer.StopTimer();


        bool loaded = save == null ? _levelLoader.TryLoad(data, out string error) : _levelLoader.TryLoadSave(data, save, out error);

        if (!loaded)
        {
            FailLevel(error);
            return false;
        }

        _levelData = data;
        _currentLevelIndex = index;

        _passengerController.Initialize(
            _levelLoader.GridBuilder,
            _levelLoader.PassengerSpawner,
            _levelLoader.BusQueue,
            _levelLoader.WaitingSlots
        );

        _levelTimer.Initialize(save == null ? data.TimeLimit : save.RemainingTime);

        _hasLoadedLevel = true;

        if (save == null || save.State == LevelState.Ready)
        {
            SetState(LevelState.Ready);
        }
        else if (save.State == LevelState.Completed)
        {
            SetState(LevelState.Completed);
        }
        else if (save.RemainingTime <= 0f)
        {
            FailLevel("Time is up.");
        }
        else
        {
            SetState(LevelState.Ready);
        }

        return true;
    }

    public void StartLevel()
    {
        if (State != LevelState.Ready && State != LevelState.Paused)
        {
            return;
        }

        SetState(LevelState.Playing);

        _passengerController.SetInputEnabled(true);

        if (State != LevelState.Playing) return;

        _levelTimer.StartTimer();
    }

    public void PauseLevel()
    {
        if (State != LevelState.Playing) return;

        _passengerController.SetInputEnabled(false);
        _levelTimer.StopTimer();

        SetState(LevelState.Paused);
    }

    public void RestartLevel()
    {
        if (_currentLevelIndex < 0) return;

        LoadLevel(_currentLevelIndex);
    }

    public void NextLevel()
    {
        if (State != LevelState.Completed) return;
        if (!HasNextLevel) return;

        LoadLevel(_currentLevelIndex + 1);
    }

    private void CompleteLevel()
    {
        if (State != LevelState.Playing) return;

        _passengerController.SetInputEnabled(false);
        _levelTimer.StopTimer();

        SetState(LevelState.Completed);

        Debug.Log($"Level {CurrentLevelNumber} completed.");
    }

    private void FailLevel(string reason)
    {
        if (State == LevelState.Completed || State == LevelState.Failed)
        {
            return;
        }

        if (_passengerController != null)
        {
            _passengerController.SetInputEnabled(false);
            _passengerController.CancelMovements();
        }

        if (_levelTimer != null)
        {
            _levelTimer.StopTimer();
        }

        SetState(LevelState.Failed);

        Debug.LogWarning(reason);
    }

    private void HandleTimeExpired()
    {
        if (State != LevelState.Playing) return;

        FailLevel("Time is up.");
    }

    private void SetState(LevelState state)
    {
        if (State == state) return;

        State = state;
        StateChanged?.Invoke(State);
    }

    private void SaveCurrentGame()
    {
        if (!_hasLoadedLevel || _levelData == null)
            return;

        if (State == LevelState.None || State == LevelState.Loading)
            return;

        if (State == LevelState.Failed)
        {
            SaveManager.Save(new SaveData
            {
                LevelNumber = CurrentLevelNumber,
                State = LevelState.Failed,
                RestartOnLoad = true,
                IsSoundEnabled = _soundManager.IsSoundEnabled,
                IsVibrationEnabled = _vibrationManager.IsVibrationEnabled
            });

            return;
        }

        SaveData save = _passengerController.CreateSave(
            CurrentLevelNumber,
            _levelTimer.RemainingTime,
            State,
            _soundManager.IsSoundEnabled
        );

        save.IsVibrationEnabled = _vibrationManager.IsVibrationEnabled;

        SaveManager.Save(save);
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