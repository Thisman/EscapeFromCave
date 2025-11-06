using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BattleAbilityItemView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private float highlightScaleMultiplier = 1.1f;
    [SerializeField] private float highlightTweenDuration = 0.2f;
    [SerializeField] private Ease highlightTweenEase = Ease.OutBack;

    public event Action<BattleAbilityDefinitionSO> OnClick;

    private BattleAbilityDefinitionSO _definition;
    private Vector3 _initialScale;
    private Tween _highlightTween;

    public BattleAbilityDefinitionSO Definition => _definition;

    private void Awake()
    {
        _initialScale = new Vector3(1, 1, 1);
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        KillHighlightTween();
        transform.localScale = _initialScale;
    }

    public void Render(BattleAbilityDefinitionSO abilityDefinition)
    {
        _definition = abilityDefinition;

        ResetHighlight(force: true);

        if (icon != null)
        {
            icon.sprite = _definition != null ? _definition.Icon : null;
        }

        SetInteractable(true);
    }

    public void Highlight()
    {
        TweenScale(_initialScale * highlightScaleMultiplier);
    }

    public void ResetHighlight()
    {
        ResetHighlight(force: false);
    }

    private void HandleClick()
    {
        OnClick?.Invoke(_definition);
    }

    public void SetInteractable(bool interactable)
    {
        if (button == null)
            return;

        button.interactable = interactable;
    }

    private void ResetHighlight(bool force)
    {
        if (force || !gameObject.activeInHierarchy || highlightTweenDuration <= 0f)
        {
            KillHighlightTween();
            transform.localScale = _initialScale;
            return;
        }

        TweenScale(_initialScale);
    }

    private void TweenScale(Vector3 targetScale)
    {
        if (!gameObject.activeInHierarchy || highlightTweenDuration <= 0f)
        {
            KillHighlightTween();
            transform.localScale = targetScale;
            return;
        }

        KillHighlightTween();
        _highlightTween = transform
            .DOScale(targetScale, highlightTweenDuration)
            .SetEase(highlightTweenEase)
            .OnComplete(() => _highlightTween = null);
    }

    private void KillHighlightTween()
    {
        if (_highlightTween == null)
            return;

        _highlightTween.Kill();
        _highlightTween = null;
    }
}
