using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private LevelTimer _levelTimer;

    [Header("Buttons")]
    [SerializeField] private GameObject _startButton;

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

        if(_startButton != null)
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

        if (_failPanel != null) {
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

    public void HandleExitClicked()
    {
        Application.Quit();
    }
}