using UnityEngine;

public class LevelTimer : MonoBehaviour
{
    public float RemainingTime { get; private set; }
    private bool _isRunning;

    private void OnEnable()
    {
        GameEvents.LevelLoaded += HandleLevelLoaded;
        GameEvents.LevelStateChanged += HandleLevelStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.LevelLoaded -= HandleLevelLoaded;
        GameEvents.LevelStateChanged -= HandleLevelStateChanged;
    }

    private void HandleLevelLoaded(LevelData data, float timeLimit, LevelState state)
    {
        Initialize(timeLimit);
    }

    private void HandleLevelStateChanged(LevelState state)
    {
        if (state == LevelState.Playing) StartTimer();
        else StopTimer();
    }

    public void Initialize(float timeLimit)
    {
        _isRunning = false;
        RemainingTime = timeLimit;
        GameEvents.TriggerTimerChanged(RemainingTime);
    }

    private void StartTimer()
    {
        if (RemainingTime > 0f) _isRunning = true;
    }

    private void StopTimer() => _isRunning = false;

    private void Update()
    {
        if (!_isRunning) return;

        RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
        GameEvents.TriggerTimerChanged(RemainingTime);

        if (RemainingTime <= 0f)
        {
            _isRunning = false;
            GameEvents.TriggerTimeExpired();
        }
    }
}
