using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public sealed class ArmyRoasterSquadView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private Transform _scaleTarget;
    [SerializeField] private float _hoverScale = 1.05f;
    [SerializeField] private float _hoverDuration = 0.2f;
    [SerializeField] private Ease _hoverEase = Ease.OutBack;
    [SerializeField] private float _returnDuration = 0.2f;
    [SerializeField] private Ease _returnEase = Ease.OutBack;

    private Tween _scaleTween;
    private Transform _cachedScaleTarget;
    private Vector3 _defaultScale;

    private void Awake()
    {
        _cachedScaleTarget = _scaleTarget != null ? _scaleTarget : transform;
        _defaultScale = _cachedScaleTarget.localScale;
    }

    private void OnDisable()
    {
        ResetScaleInstant();
    }

    public void Bind(IReadOnlySquadModel squad)
    {
        if (squad == null)
        {
            if (_icon) _icon.sprite = null;
            if (_countText) _countText.text = string.Empty;
            return;
        }

        if (_icon)
            _icon.sprite = squad.UnitDefinition != null ? squad.UnitDefinition.Icon : null;

        if (_countText)
            _countText.text = squad.Count.ToString();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_cachedScaleTarget == null)
            return;

        PlayScaleTween(_defaultScale * _hoverScale, _hoverDuration, _hoverEase);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_cachedScaleTarget == null)
            return;

        PlayScaleTween(_defaultScale, _returnDuration, _returnEase);
    }

    private void PlayScaleTween(Vector3 targetScale, float duration, Ease ease)
    {
        _scaleTween?.Kill();
        _scaleTween = _cachedScaleTarget.DOScale(targetScale, duration).SetEase(ease);
    }

    private void ResetScaleInstant()
    {
        _scaleTween?.Kill();
        _scaleTween = null;

        if (_cachedScaleTarget != null)
            _cachedScaleTarget.localScale = _defaultScale;
    }
}
