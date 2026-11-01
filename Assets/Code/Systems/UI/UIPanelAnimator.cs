using System;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class UIPanelAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform _contentRoot;

    [Header("Show Animation")]
    [SerializeField] private float _showDuration = 0.35f;
    [SerializeField] private Ease _showEase = Ease.OutBack;
    [SerializeField] private float _startScale = 0f;

    [Header("Hide Animation")]
    [SerializeField] private float _hideDuration = 0.2f;
    [SerializeField] private Ease _hideEase = Ease.InBack;
    [SerializeField] private float _endScale = 0f;

    private Tween _scaleTween;
    
    public void Show(Action onComplete = null)
    {
        KillTween();
        gameObject.SetActive(true);

        if (_contentRoot == null)
        {
            onComplete?.Invoke();
            return;
        }

        _contentRoot.localScale = Vector3.one * _startScale;
        _scaleTween = _contentRoot
            .DOScale(Vector3.one, _showDuration)
            .SetEase(_showEase)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void Hide(Action onComplete = null, bool immediate = false)
    {
        KillTween();

        if (immediate || !gameObject.activeInHierarchy)
        {
            if (_contentRoot != null)
            {
                _contentRoot.localScale = Vector3.one * _endScale;
            }

            gameObject.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        if (_contentRoot == null)
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        _scaleTween = _contentRoot
            .DOScale(Vector3.one * _endScale, _hideDuration)
            .SetEase(_hideEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }

    private void KillTween()
    {
        if (_scaleTween != null && _scaleTween.IsActive())
        {
            _scaleTween.Kill();
            _scaleTween = null;
        }

        if (_contentRoot != null)
        {
            _contentRoot.DOKill();
        }
    }

    private void OnDestroy()
    {
        KillTween();
    }
}
