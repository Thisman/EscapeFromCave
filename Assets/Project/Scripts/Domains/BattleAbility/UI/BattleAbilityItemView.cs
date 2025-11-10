using System;
using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleAbilityItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private Button _button;
    [SerializeField] private float _highlightScaleMultiplier = 1.1f;
    [SerializeField] private float _highlightTweenDuration = 0.2f;
    [SerializeField] private Ease _highlightTweenEase = Ease.OutBack;
    [SerializeField] private GameObject _descriptionRoot;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    public event Action<BattleAbilityItemView, BattleAbilitySO> OnClick;

    private BattleAbilitySO _definition;
    private Vector3 _initialScale;
    private Tween _highlightTween;

    public BattleAbilitySO Definition => _definition;

    private void Awake()
    {
        _initialScale = new Vector3(1, 1, 1);
    }

    private void OnEnable()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        KillHighlightTween();
        _button.transform.localScale = _initialScale;
        HideDescription();
    }

    public void Render(BattleAbilitySO abilityDefinition)
    {
        _definition = abilityDefinition;

        ResetHighlight(force: true);

        if (_icon != null)
        {
            _icon.sprite = _definition != null ? _definition.Icon : null;
        }

        SetInteractable(true);
        UpdateDescriptionText();
        HideDescription();
    }

    public void Highlight()
    {
        TweenScale(_initialScale * _highlightScaleMultiplier);
    }

    public void ResetHighlight()
    {
        ResetHighlight(force: false);
    }

    private void HandleClick()
    {
        OnClick?.Invoke(this, _definition);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_definition == null)
            return;

        UpdateDescriptionText();
        ShowDescription();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideDescription();
    }

    public void SetInteractable(bool interactable)
    {
        if (_button == null)
            return;

        _button.interactable = interactable;
    }

    private void ResetHighlight(bool force)
    {
        if (force || !gameObject.activeInHierarchy || _highlightTweenDuration <= 0f)
        {
            KillHighlightTween();
            _button.transform.localScale = _initialScale;
            return;
        }

        TweenScale(_initialScale);
    }

    private void TweenScale(Vector3 targetScale)
    {
        if (!_button.gameObject.activeInHierarchy || _highlightTweenDuration <= 0f)
        {
            KillHighlightTween();
            _button.transform.localScale = targetScale;
            return;
        }

        KillHighlightTween();
        _highlightTween = _button.transform
            .DOScale(targetScale, _highlightTweenDuration)
            .SetEase(_highlightTweenEase)
            .OnComplete(() => _highlightTween = null);
    }

    private void KillHighlightTween()
    {
        if (_highlightTween == null)
            return;

        _highlightTween.Kill();
        _highlightTween = null;
    }

    private void ShowDescription()
    {
        _descriptionRoot.SetActive(true);
    }

    private void HideDescription()
    {
        _descriptionRoot.SetActive(false);
    }

    private void UpdateDescriptionText()
    {
        if (_descriptionText == null || _definition == null)
            return;

        _descriptionText.text = FormatDescription(_definition);
    }

    private string FormatDescription(BattleAbilitySO abilityDefinition)
    {
        string cooldownLabel = GetCooldownText(abilityDefinition.Cooldown);
        return $"{abilityDefinition.AbilityName}\n{abilityDefinition.Description}\nПерезарядка: {abilityDefinition.Cooldown} {cooldownLabel}";
    }

    private string GetCooldownText(int cooldown)
    {
        int absoluteCooldown = Mathf.Abs(cooldown);
        int lastTwoDigits = absoluteCooldown % 100;
        int lastDigit = absoluteCooldown % 10;

        if (lastDigit == 1 && lastTwoDigits != 11)
        {
            return "раунд";
        }

        if (lastDigit >= 2 && lastDigit <= 4 && (lastTwoDigits < 12 || lastTwoDigits > 14))
        {
            return "раунда";
        }

        return "раундов";
    }
}
