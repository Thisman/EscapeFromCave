using System;
using System.Collections;
using UnityEngine;

public class BattleSquadAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BattleSquadController _unitController;
    [SerializeField] private Color _damageFlashColor = Color.red;
    [SerializeField, Min(0f)] private float _damageFlashDuration = 0.5f;
    [SerializeField, Min(0f)] private float _damageFlashFrequency = 6f;

    private Coroutine _flashRoutine;
    private Action _flashCompletion;
    private Color _flashRestoreColor = Color.white;

    private void Start()
    {
        if (_spriteRenderer == null || _unitController == null)
            return;

        var model = _unitController.Model;
        if (model?.Definition != null)
            _spriteRenderer.sprite = model.Definition.Icon;
        _flashRestoreColor = _spriteRenderer.color;
    }

    private void OnDisable()
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            RestoreSpriteColor();
            CompleteFlash();
        }
    }

    public void PlayDamageFlash(Action onComplete)
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

        _flashCompletion = onComplete;
        _flashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    public void CancelDamageFlash()
    {
        if (_flashRoutine == null)
            return;

        StopCoroutine(_flashRoutine);
        RestoreSpriteColor();
        _flashRoutine = null;
        _flashCompletion = null;
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
}
