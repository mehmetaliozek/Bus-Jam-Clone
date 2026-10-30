using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public event Action<bool> SoundEnabledChanged;

    [Header("References")]
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private PassengerController _passengerController;
    [SerializeField] private BusQueue _busQueue;

    [Header("Clips")]
    [SerializeField] private AudioClip _levelReadyClip;
    [SerializeField] private AudioClip _passengerMoveClip;
    [SerializeField] private AudioClip _busDepartureClip;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)]
    private float _levelReadyVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float _passengerMoveVolume = 0.7f;

    [SerializeField, Range(0f, 1f)]
    private float _busDepartureVolume = 1f;

    private AudioSource _audioSource;
    private bool _hasPlayedStartSound;

    private bool _isSoundEnabled = true;

    public bool IsSoundEnabled => _isSoundEnabled;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        if (_levelManager != null)
        {
            _levelManager.StateChanged += HandleLevelStateChanged;
        }

        if (_passengerController != null)
        {
            _passengerController.PassengerMoveStarted += HandlePassengerMoveStarted;
        }

        if (_busQueue != null)
        {
            _busQueue.BusDepartureStarted += HandleBusDepartureStarted;
        }
    }

    private void OnDisable()
    {
        if (_levelManager != null)
        {
            _levelManager.StateChanged -= HandleLevelStateChanged;
        }

        if (_passengerController != null)
        {
            _passengerController.PassengerMoveStarted -= HandlePassengerMoveStarted;
        }

        if (_busQueue != null)
        {
            _busQueue.BusDepartureStarted -= HandleBusDepartureStarted;
        }

        StopSounds();
    }

    private void HandleLevelStateChanged(LevelState state)
    {
        switch (state)
        {
            case LevelState.Loading:
                _hasPlayedStartSound = false;
                StopSounds();
                break;

            case LevelState.Playing:
                if (!_hasPlayedStartSound)
                {
                    _hasPlayedStartSound = true;
                    PlaySound(_levelReadyClip, _levelReadyVolume);
                }
                break;

            case LevelState.Paused:
            case LevelState.Failed:
                StopSounds();
                break;
        }
    }

    private void HandlePassengerMoveStarted()
    {
        PlaySound(_passengerMoveClip, _passengerMoveVolume);
    }

    private void HandleBusDepartureStarted()
    {
        PlaySound(_busDepartureClip, _busDepartureVolume);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (!isActiveAndEnabled || !_isSoundEnabled || _audioSource == null || clip == null)
            return;

        _audioSource.PlayOneShot(clip, volume);
    }

    private void StopSounds()
    {
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }
    }

    public void SetSoundEnabled()
    {
        SetSoundEnabled(!_isSoundEnabled);
    }

    public void SetSoundEnabled(bool isEnabled)
    {
        _isSoundEnabled = isEnabled;

        if (!isEnabled)
        {
            StopSounds();
        }

        SoundEnabledChanged?.Invoke(isEnabled);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            StopSounds();
        }
    }
}