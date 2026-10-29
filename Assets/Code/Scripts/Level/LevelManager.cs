using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public event Action<LevelState> StateChanged;
    public event Action LevelCompleted;
    public event Action<string> LevelFailed;

    public LevelState State { get; private set; } = LevelState.None;

    [SerializeField] private LevelData _levelData;

    [SerializeField] private LevelLoader _levelLoader;
    [SerializeField] private PassengerController _passengerController;
    [SerializeField] private LevelTimer _levelTimer;

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
            _levelTimer.StartTimer();
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
        LoadLevel(_levelData);
    }

    public bool LoadLevel(LevelData data)
    {
        SetState(LevelState.Loading);

        if (_levelLoader == null || _passengerController == null || _levelTimer == null)
        {
            FailLevel("LevelManager references are missing.");
            return false;
        }

        _passengerController.Clear();
        _levelTimer.StopTimer();

        if (!_levelLoader.TryLoad(data, out string error))
        {
            FailLevel(error);
            return false;
        }

        _levelData = data;

        _passengerController.Initialize(_levelLoader.GridBuilder, _levelLoader.PassengerSpawner, _levelLoader.BusQueue, _levelLoader.WaitingSlots);

        _levelTimer.Initialize(data.TimeLimit);

        SetState(LevelState.Ready);

        StartLevel();

        return true;
    }

    public void StartLevel()
    {
        if (State != LevelState.Ready) return;

        SetState(LevelState.Playing);

        _passengerController.SetInputEnabled(true);
        _levelTimer.StartTimer();
    }

    public void RestartLevel()
    {
        LoadLevel(_levelData);
    }

    private void CompleteLevel()
    {
        if (State != LevelState.Playing) return;

        _passengerController.SetInputEnabled(false);
        _levelTimer.StopTimer();

        SetState(LevelState.Completed);

        Debug.Log("Level completed.");
        LevelCompleted?.Invoke();
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
        }

        if (_levelTimer != null)
        {
            _levelTimer.StopTimer();
        }

        SetState(LevelState.Failed);

        Debug.LogWarning(reason);
        LevelFailed?.Invoke(reason);
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
}