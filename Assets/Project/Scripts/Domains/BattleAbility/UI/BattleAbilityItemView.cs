using System;
using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleAbilityItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private float highlightScaleMultiplier = 1.1f;
    [SerializeField] private float highlightTweenDuration = 0.2f;
    [SerializeField] private Ease highlightTweenEase = Ease.OutBack;
    [SerializeField] private GameObject descriptionRoot;
    [SerializeField] private TextMeshProUGUI descriptionText;

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
        HideDescription();
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
        UpdateDescriptionText();
        HideDescription();
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

    private void ShowDescription()
    {
        if (descriptionRoot != null)
        {
            descriptionRoot.SetActive(true);
        }
    }

    private void HideDescription()
    {
        if (descriptionRoot != null)
        {
            descriptionRoot.SetActive(false);
        }
    }

    private void UpdateDescriptionText()
    {
        if (descriptionText == null || _definition == null)
            return;

        descriptionText.text = FormatDescription(_definition);
    }

    private string FormatDescription(BattleAbilityDefinitionSO abilityDefinition)
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
