using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private LevelTimer _levelTimer;
    [SerializeField] private SoundManager _soundManager;
    [SerializeField] private VibrationManager _vibrationManager;

    [Header("Buttons")]
    [SerializeField] private GameObject _startButton;
    [SerializeField] private GameObject _soundDisable;
    [SerializeField] private GameObject _vibrationDisable;

    [Header("Texts")]
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _timerText;

    [Header("Panels")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _winPanel;
    [SerializeField] private GameObject _failPanel;

    private void OnEnable()
    {
        if (_levelManager != null)
        {
            _levelManager.StateChanged += HandleStateChanged;
        }

        if (_levelTimer != null)
        {
            _levelTimer.TimeChanged += HandleTimeChanged;
        }

        if (_soundManager != null)
        {
            _soundManager.SoundEnabledChanged += HandleSoundEnabledChanged;

            HandleSoundEnabledChanged(_soundManager.IsSoundEnabled);
        }

        if (_vibrationManager != null)
        {
            _vibrationManager.VibrationEnabledChanged += HandleVibrationEnabledChanged;

            HandleVibrationEnabledChanged(_vibrationManager.IsVibrationEnabled);
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (_levelManager != null)
        {
            _levelManager.StateChanged -= HandleStateChanged;
        }

        if (_levelTimer != null)
        {
            _levelTimer.TimeChanged -= HandleTimeChanged;
        }

        if (_soundManager != null)
        {
            _soundManager.SoundEnabledChanged -= HandleSoundEnabledChanged;
        }

        if (_vibrationManager != null)
        {
            _vibrationManager.VibrationEnabledChanged -= HandleVibrationEnabledChanged;
        }
    }

    private void Refresh()
    {
        if (_levelManager != null)
        {
            HandleStateChanged(_levelManager.State);
        }

        if (_levelTimer != null)
        {
            HandleTimeChanged(_levelTimer.RemainingTime);
        }
    }

    private void HandleStateChanged(LevelState state)
    {
        if (_levelText != null)
        {
            _levelText.SetText("Level {0}", _levelManager.CurrentLevelNumber);
        }

        if (_startButton != null)
        {
            _startButton.SetActive(state == LevelState.Ready);
        }

        if (_pausePanel != null)
        {
            _pausePanel.SetActive(state == LevelState.Paused);
        }

        if (_winPanel != null)
        {
            _winPanel.SetActive(state == LevelState.Completed);
        }

        if (_failPanel != null)
        {
            _failPanel.SetActive(state == LevelState.Failed);
        }
    }

    private void HandleTimeChanged(float remainingTime)
    {
        if (_timerText == null) return;

        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        _timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void HandleSoundEnabledChanged(bool isEnabled)
    {
        if (_soundDisable != null)
        {
            _soundDisable.SetActive(!isEnabled);
        }
    }

    private void HandleVibrationEnabledChanged(bool isEnabled)
    {
        if (_vibrationDisable != null)
        {
            _vibrationDisable.SetActive(!isEnabled);
        }
    }

    public void HandleStartClicked()
    {
        if (_levelManager == null) return;
        _levelManager.StartLevel();
    }

    public void HandlePauseClicked()
    {
        if (_levelManager == null) return;
        _levelManager.PauseLevel();
    }

    public void HandleRestartClicked()
    {
        if (_levelManager == null) return;
        _levelManager.RestartLevel();
    }

    public void HandleNextLevelClicked()
    {
        if (_levelManager == null) return;
        _levelManager.NextLevel();
    }

    public void HandleSoundClicked()
    {
        if (_soundManager == null)
            return;

        _soundManager.SetSoundEnabled();
    }

    public void HandleVibrateClicked()
    {
        if (_vibrationManager == null)
            return;

        _vibrationManager.SetVibrationEnabled();
    }

    public void HandleExitClicked()
    {
        Application.Quit();
    }
}