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
    [SerializeField] private float _scaleAmplitude = 0.03f;
    [SerializeField] private float _scaleFrequency = 6f;

    [SerializeField] private Color _damageFlashColor = Color.red;
    [SerializeField] private Color _dodgeFlashColor = Color.white;
    [SerializeField, Min(0f)] private float _damageFlashDuration = 0.5f;
    [SerializeField, Min(0f)] private float _damageFlashFrequency = 6f;
    [SerializeField] private Transform _shakeTarget;
    [SerializeField, Min(0f)] private float _damageShakeAmplitude = 0.1f;
    [SerializeField] private Vector2 _damageTextOffset = new(0f, 50f);
    [SerializeField, Range(0f, 1f)] private float _unavailableAlpha = 0.35f;

    private Coroutine _flashRoutine;
    private Coroutine _damageTextRoutine;
    private Action _flashCompletion;
    private Color _flashRestoreColor = Color.white;
    private Vector3 _damageShakeInitialLocalPosition;
    private Vector2 _damageTextInitialPosition;
    private Color _damageTextVisibleColor = Color.white;
    private Color _damageTextHiddenColor = new(1f, 1f, 1f, 0f);
    private Vector3 _initialScale;
    private bool _isScaleAnimationPaused;
    private float _scaleAnimationStartTime;
    private bool _isAvailabilityOverrideActive;
    private Color _availabilitySpriteRestoreColor;
    private Color _availabilityCountRestoreColor;
    private Color _availabilityDamageTextRestoreColor;

    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        _initialScale = transform.localScale;
        _isScaleAnimationPaused = false;
        ScheduleScaleAnimation();
    }

    private void Start()
    {
        _flashRestoreColor = _spriteRenderer.color;
        _spriteRenderer.sprite = _unitController.GetSquadModel().Icon;

        if (_shakeTarget == null)
            _shakeTarget = _spriteRenderer != null ? _spriteRenderer.transform : transform;

        if (_shakeTarget != null)
            _damageShakeInitialLocalPosition = _shakeTarget.localPosition;

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
            RestoreShakeTarget();
            CompleteFlash();
        }

        if (_damageTextRoutine != null)
        {
            StopCoroutine(_damageTextRoutine);
            ResetDamageText();
        }

        _unitController.GetSquadModel().Changed -= HandleModelChanged;

        transform.localScale = _initialScale;
        _isScaleAnimationPaused = false;

        ResetAvailabilityVisual();
    }

    private void Update()
    {
        AnimateScale();
    }

    public void PlayDamageFlash(Action onComplete)
    {
        PlayDamageFlash(0, onComplete);
    }

    public void PlayDamageFlash(int damage, Action onComplete)
    {
        StartFlashRoutine(_damageFlashColor, true, damage > 0, damage, onComplete);
    }

    public void PlayDodgeFlash(Action onComplete)
    {
        StartFlashRoutine(_dodgeFlashColor, false, false, 0, onComplete);
    }

    public void CancelDamageFlash()
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            RestoreSpriteColor();
            RestoreShakeTarget();
            _flashRoutine = null;
            _flashCompletion = null;
            ResumeScaleAnimation();
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

    public void SetAvailabilityVisual(bool isAvailable)
    {
        if (isAvailable)
        {
            ResetAvailabilityVisual();
            return;
        }

        if (_isAvailabilityOverrideActive)
            return;

        float alphaMultiplier = Mathf.Clamp01(_unavailableAlpha);

        if (_spriteRenderer != null)
        {
            _availabilitySpriteRestoreColor = _spriteRenderer.color;
            var color = _availabilitySpriteRestoreColor;
            color.a *= alphaMultiplier;
            _spriteRenderer.color = color;
        }

        if (_countTextUI != null)
        {
            _availabilityCountRestoreColor = _countTextUI.color;
            var color = _availabilityCountRestoreColor;
            color.a *= alphaMultiplier;
            _countTextUI.color = color;
        }

        if (_damageTextUI != null)
        {
            _availabilityDamageTextRestoreColor = _damageTextUI.color;
            var color = _availabilityDamageTextRestoreColor;
            color.a *= alphaMultiplier;
            _damageTextUI.color = color;
        }

        _isAvailabilityOverrideActive = true;
    }

    public void ResetAvailabilityVisual()
    {
        if (!_isAvailabilityOverrideActive)
            return;

        if (_spriteRenderer != null)
            _spriteRenderer.color = _availabilitySpriteRestoreColor;

        if (_countTextUI != null)
            _countTextUI.color = _availabilityCountRestoreColor;

        if (_damageTextUI != null)
            _damageTextUI.color = _availabilityDamageTextRestoreColor;

        _isAvailabilityOverrideActive = false;
    }

    private void StartFlashRoutine(Color flashColor, bool useShake, bool showDamageText, int damage, Action onComplete)
    {
        if (_spriteRenderer == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (_shakeTarget == null)
            _shakeTarget = _spriteRenderer != null ? _spriteRenderer.transform : transform;

        if (!isActiveAndEnabled)
        {
            RestoreSpriteColor();
            if (useShake)
                RestoreShakeTarget();
            onComplete?.Invoke();
            return;
        }

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            RestoreSpriteColor();
            RestoreShakeTarget();
            CompleteFlash();
        }

        if (_damageTextRoutine != null)
        {
            StopCoroutine(_damageTextRoutine);
            ResetDamageText();
        }

        if (showDamageText && _damageTextUI != null && damage > 0)
            _damageTextRoutine = StartCoroutine(DamageTextRoutine(damage));

        if (_shakeTarget != null)
            _damageShakeInitialLocalPosition = _shakeTarget.localPosition;

        _flashCompletion = onComplete;
        PauseScaleAnimation();
        _flashRoutine = StartCoroutine(FlashRoutine(flashColor, useShake));
    }

    private IEnumerator FlashRoutine(Color flashColor, bool useShake)
    {
        _flashRestoreColor = _spriteRenderer.color;

        float duration = Mathf.Max(_damageFlashDuration, 0f);
        float frequency = Mathf.Max(_damageFlashFrequency, Mathf.Epsilon);

        var shakeTarget = _shakeTarget;

        if (duration <= Mathf.Epsilon)
        {
            RestoreSpriteColor();
            if (useShake)
                RestoreShakeTarget();
            CompleteFlash();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.PingPong(elapsed * frequency, 1f);
            _spriteRenderer.color = Color.Lerp(_flashRestoreColor, flashColor, t);
            if (useShake)
                ApplyShake(shakeTarget);
            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreSpriteColor();
        if (useShake)
            RestoreShakeTarget();
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

    private void RestoreShakeTarget()
    {
        if (_shakeTarget != null)
            _shakeTarget.localPosition = _damageShakeInitialLocalPosition;
    }

    private void CompleteFlash()
    {
        var completion = _flashCompletion;
        _flashCompletion = null;
        _flashRoutine = null;
        completion?.Invoke();
        ResumeScaleAnimation();
    }

    private void ApplyShake(Transform target)
    {
        if (target == null)
            return;

        float amplitude = Mathf.Max(0f, _damageShakeAmplitude);
        if (amplitude <= 0f)
            return;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * amplitude;
        target.localPosition = _damageShakeInitialLocalPosition + new Vector3(offset.x, offset.y, 0f);
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

    private void AnimateScale()
    {
        if (_isScaleAnimationPaused)
            return;

        if (_scaleAmplitude <= 0f || _scaleFrequency <= 0f)
        {
            transform.localScale = _initialScale;
            return;
        }

        float elapsed = Time.time - _scaleAnimationStartTime;
        if (elapsed < 0f)
        {
            transform.localScale = _initialScale;
            return;
        }

        float scaleOffset = Mathf.Sin(elapsed * _scaleFrequency) * _scaleAmplitude;
        var targetScale = _initialScale;
        targetScale.y = _initialScale.y * (1f + scaleOffset);
        transform.localScale = targetScale;
    }

    private void PauseScaleAnimation()
    {
        _isScaleAnimationPaused = true;
        transform.localScale = _initialScale;
    }

    private void ResumeScaleAnimation()
    {
        _isScaleAnimationPaused = false;
        _scaleAnimationStartTime = Time.time;
    }

    private void ScheduleScaleAnimation()
    {
        _scaleAnimationStartTime = Time.time + UnityEngine.Random.Range(0f, 0.5f);
    }
}
