using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip _levelReadyClip;
    [SerializeField] private AudioClip _passengerMoveClip;
    [SerializeField] private AudioClip _busDepartureClip;
    [SerializeField] private AudioClip _buttonClickClip;
    [SerializeField] private AudioClip _levelCompletedClip;
    [SerializeField] private AudioClip _levelFailedClip;


    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float _levelReadyVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float _passengerMoveVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float _busDepartureVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float _buttonClickVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float _levelCompletedVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float _levelFailedVolume = 1f;

    private AudioSource _audioSource;
    private bool _hasPlayedStartSound;
    private bool _isSoundEnabled = true;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        GameEvents.LevelStateChanged += HandleLevelStateChanged;
        GameEvents.PassengerMoveStarted += HandlePassengerMoveStarted;
        GameEvents.BusDepartureStarted += HandleBusDepartureStarted;
        GameEvents.RequestToggleSound += HandleRequestToggleSound;
        GameEvents.SoundEnabledChanged += HandleSoundEnabledChanged;
        GameEvents.ButtonClicked += HandleButtonClicked;
    }

    private void OnDisable()
    {
        GameEvents.LevelStateChanged -= HandleLevelStateChanged;
        GameEvents.PassengerMoveStarted -= HandlePassengerMoveStarted;
        GameEvents.BusDepartureStarted -= HandleBusDepartureStarted;
        GameEvents.RequestToggleSound -= HandleRequestToggleSound;
        GameEvents.SoundEnabledChanged -= HandleSoundEnabledChanged;
        GameEvents.ButtonClicked -= HandleButtonClicked;
        StopSounds();
    }

    private void HandleLevelStateChanged(LevelState state)
    {
        switch (state)
        {
            case LevelState.None:
                break;
            case LevelState.Loading:
                _hasPlayedStartSound = false;
                StopSounds();
                break;
            case LevelState.Ready:
                break;
            case LevelState.Playing:
                if (!_hasPlayedStartSound)
                {
                    _hasPlayedStartSound = true;
                    PlaySound(_levelReadyClip, _levelReadyVolume);
                }
                break;
            case LevelState.Completed:
                PlaySound(_levelCompletedClip, _levelCompletedVolume);
                break;
            case LevelState.Failed:
                PlaySound(_levelFailedClip, _levelFailedVolume);
                break;
            case LevelState.Paused:
                StopSounds();
                break;
            default:
                break;
        }
    }

    private void HandlePassengerMoveStarted() => PlaySound(_passengerMoveClip, _passengerMoveVolume);
    private void HandleBusDepartureStarted() => PlaySound(_busDepartureClip, _busDepartureVolume);
    private void HandleButtonClicked() => PlaySound(_buttonClickClip, _buttonClickVolume);

    private void HandleRequestToggleSound()
    {
        GameEvents.TriggerSoundEnabledChanged(!_isSoundEnabled);
    }

    private void HandleSoundEnabledChanged(bool isEnabled)
    {
        if (_isSoundEnabled == isEnabled) return;
        _isSoundEnabled = isEnabled;
        if (!isEnabled)
        {
            StopSounds();
        }
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (isActiveAndEnabled && _isSoundEnabled && _audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip, volume);
        }
    }

    private void StopSounds() => _audioSource.Stop();

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            StopSounds();
        }
    }
}
