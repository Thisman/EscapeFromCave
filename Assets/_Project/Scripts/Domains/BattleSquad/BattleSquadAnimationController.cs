using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class BattleSquadAnimationController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countTextUI;
    [SerializeField] private TextMeshProUGUI _damageTextUI;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BattleSquadController _unitController;

    [SerializeField] private Color _damageFlashColor = Color.red;
    [SerializeField, Min(0f)] private float _damageFlashDuration = 0.5f;
    [SerializeField, Min(0f)] private float _damageFlashFrequency = 6f;
    [SerializeField] private Vector2 _damageTextOffset = new(0f, 50f);

    private Coroutine _flashRoutine;
    private Coroutine _damageTextRoutine;
    private Action _flashCompletion;
    private Color _flashRestoreColor = Color.white;
    private Vector2 _damageTextInitialPosition;
    private Color _damageTextVisibleColor = Color.white;
    private Color _damageTextHiddenColor = new(1f, 1f, 1f, 0f);

    private void Start()
    {
        _flashRestoreColor = _spriteRenderer.color;
        _spriteRenderer.sprite = _unitController.GetSquadModel().Definition.Icon;

        _unitController.GetSquadModel().Changed += HandleModelChanged;
        HandleModelChanged(_unitController.GetSquadModel());

        if (_damageTextUI != null)
        {
            var rectTransform = _damageTextUI.rectTransform;
            _damageTextInitialPosition = rectTransform.anchoredPosition;
            _damageTextVisibleColor = _damageTextUI.color;
            _damageTextHiddenColor = _damageTextVisibleColor;
            _damageTextHiddenColor.a = 0f;
            _damageTextUI.color = _damageTextHiddenColor;
            _damageTextUI.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            RestoreSpriteColor();
            CompleteFlash();
        }

        if (_damageTextRoutine != null)
        {
            StopCoroutine(_damageTextRoutine);
            ResetDamageText();
        }

        _unitController.GetSquadModel().Changed -= HandleModelChanged;
    }

    public void PlayDamageFlash(Action onComplete)
    {
        PlayDamageFlash(0, onComplete);
    }

    public void PlayDamageFlash(int damage, Action onComplete)
    {
        if (_spriteRenderer == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (!isActiveAndEnabled)
        {
            _spriteRenderer.color = _flashRestoreColor;
            onComplete?.Invoke();
            return;
        }

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            RestoreSpriteColor();
            CompleteFlash();
        }

        if (_damageTextRoutine != null)
        {
            StopCoroutine(_damageTextRoutine);
            ResetDamageText();
        }

        if (_damageTextUI != null && damage > 0)
            _damageTextRoutine = StartCoroutine(DamageTextRoutine(damage));

        _flashCompletion = onComplete;
        _flashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    public void CancelDamageFlash()
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            RestoreSpriteColor();
            _flashRoutine = null;
            _flashCompletion = null;
        }

        if (_damageTextRoutine != null)
        {
            StopCoroutine(_damageTextRoutine);
            ResetDamageText();
        }
    }

    public void SetFlipX(bool flipped)
    {
        if (_spriteRenderer == null)
            return;

        _spriteRenderer.flipX = flipped;
    }

    private IEnumerator DamageFlashRoutine()
    {
        _flashRestoreColor = _spriteRenderer.color;

        float duration = Mathf.Max(_damageFlashDuration, 0f);
        float frequency = Mathf.Max(_damageFlashFrequency, Mathf.Epsilon);

        if (duration <= Mathf.Epsilon)
        {
            RestoreSpriteColor();
            CompleteFlash();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.PingPong(elapsed * frequency, 1f);
            _spriteRenderer.color = Color.Lerp(_flashRestoreColor, _damageFlashColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreSpriteColor();
        CompleteFlash();
    }

    private IEnumerator DamageTextRoutine(int damage)
    {
        float duration = Mathf.Max(_damageFlashDuration, 0f);
        if (duration <= Mathf.Epsilon)
        {
            ResetDamageText();
            yield break;
        }

        var rectTransform = _damageTextUI.rectTransform;
        rectTransform.anchoredPosition = _damageTextInitialPosition;
        _damageTextUI.text = $"-{damage}hp";
        _damageTextUI.color = _damageTextVisibleColor;
        _damageTextUI.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(
                _damageTextInitialPosition,
                _damageTextInitialPosition + _damageTextOffset,
                t);

            _damageTextUI.color = Color.Lerp(_damageTextVisibleColor, _damageTextHiddenColor, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetDamageText();
        _damageTextRoutine = null;
    }

    private void RestoreSpriteColor()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.color = _flashRestoreColor;
    }

    private void CompleteFlash()
    {
        var completion = _flashCompletion;
        _flashCompletion = null;
        _flashRoutine = null;
        completion?.Invoke();
    }

    private void ResetDamageText()
    {
        if (_damageTextUI == null)
            return;

        var rectTransform = _damageTextUI.rectTransform;
        rectTransform.anchoredPosition = _damageTextInitialPosition;
        _damageTextUI.color = _damageTextHiddenColor;
        _damageTextUI.gameObject.SetActive(false);
        _damageTextRoutine = null;
    }

    private void HandleModelChanged(IReadOnlySquadModel model)
    {
        _countTextUI.text = model.Count.ToString();
    }
}
