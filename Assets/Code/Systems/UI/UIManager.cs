using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private GameObject _startButton;
    [SerializeField] private GameObject _soundDisable;
    [SerializeField] private GameObject _vibrationDisable;

    [Header("Texts")]
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _timerText;

    [Header("Panels")]
    [SerializeField] private UIPanelAnimator _pausePanel;
    [SerializeField] private UIPanelAnimator _winPanel;
    [SerializeField] private UIPanelAnimator _failPanel;

    private int _lastDisplayedSeconds = -1;

    private void Awake()
    {
        SetPanelActive(_pausePanel, false, immediate: true);
        SetPanelActive(_winPanel, false, immediate: true);
        SetPanelActive(_failPanel, false, immediate: true);
    }

    private void OnEnable()
    {
        _lastDisplayedSeconds = -1;

        GameEvents.LevelStateChanged += HandleStateChanged;
        GameEvents.LevelNumberChanged += HandleLevelNumberChanged;
        GameEvents.TimerChanged += HandleTimeChanged;
        GameEvents.SoundEnabledChanged += HandleSoundEnabledChanged;
        GameEvents.VibrationEnabledChanged += HandleVibrationEnabledChanged;
    }

    private void OnDisable()
    {
        GameEvents.LevelStateChanged -= HandleStateChanged;
        GameEvents.LevelNumberChanged -= HandleLevelNumberChanged;
        GameEvents.TimerChanged -= HandleTimeChanged;
        GameEvents.SoundEnabledChanged -= HandleSoundEnabledChanged;
        GameEvents.VibrationEnabledChanged -= HandleVibrationEnabledChanged;
    }

    private void HandleLevelNumberChanged(int levelNumber)
    {
        if (_levelText != null)
        {
            _levelText.SetText("Level {0}", levelNumber);
        }
    }

    private void HandleStateChanged(LevelState state)
    {
        if (_startButton != null)
        {
            _startButton.SetActive(state == LevelState.Ready);
        }

        SetPanelActive(_pausePanel, state == LevelState.Paused);
        SetPanelActive(_winPanel, state == LevelState.Completed);
        SetPanelActive(_failPanel, state == LevelState.Failed);
    }

    private void SetPanelActive(UIPanelAnimator animator, bool active, bool immediate = false)
    {
        if (animator != null)
        {
            if (active)
            {
                animator.Show();
            }
            else
            {
                animator.Hide(immediate: immediate);
            }
        }
    }

    private void HandleTimeChanged(float remainingTime)
    {
        if (_timerText == null) return;

        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
        if (totalSeconds == _lastDisplayedSeconds) return;
        _lastDisplayedSeconds = totalSeconds;

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

    public void HandleStartClicked() => GameEvents.TriggerRequestStartLevel();
    public void HandlePauseClicked() => GameEvents.TriggerRequestPauseLevel();
    public void HandleRestartClicked() => GameEvents.TriggerRequestRestartLevel();
    public void HandleNextLevelClicked() => GameEvents.TriggerRequestNextLevel();
    public void HandleSoundClicked() => GameEvents.TriggerRequestToggleSound();
    public void HandleVibrateClicked() => GameEvents.TriggerRequestToggleVibration();
    public void HandleExitClicked() => Application.Quit();
}
