using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public sealed class DialogController : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField, Min(0f)] private float _defaultSecondsPerCharacter = 0.05f;

    private Coroutine _typingRoutine;
    private Coroutine _displayRoutine;
    private TaskCompletionSource<bool> _displayCompletion;

    private void Awake()
    {
        if (_canvas != null)
        {
            _canvas.enabled = false;
        }

        if (_text != null)
        {
            _text.text = string.Empty;
            _text.maxVisibleCharacters = 0;
        }
    }

    public void Show(string message)
    {
        Show(message, _defaultSecondsPerCharacter);
    }

    public void Show(string message, float secondsPerCharacter)
    {
        if (_canvas == null || _text == null)
        {
            Debug.LogWarning("[DialogController] Missing canvas or text reference. Unable to show dialog.");
            return;
        }

        if (_typingRoutine != null)
        {
            StopCoroutine(_typingRoutine);
        }

        if (_displayRoutine != null)
        {
            StopCoroutine(_displayRoutine);
            _displayRoutine = null;
        }

        _displayCompletion?.TrySetResult(true);
        _displayCompletion = null;

        var messageToShow = message ?? string.Empty;
        var resolvedSecondsPerCharacter = ResolveSecondsPerCharacter(secondsPerCharacter);

        _canvas.enabled = true;
        _typingRoutine = StartCoroutine(TypeText(messageToShow, resolvedSecondsPerCharacter));
    }

    public void Hide()
    {
        if (_typingRoutine != null)
        {
            StopCoroutine(_typingRoutine);
            _typingRoutine = null;
        }

        if (_displayRoutine != null)
        {
            StopCoroutine(_displayRoutine);
            _displayRoutine = null;
        }

        if (_text != null)
        {
            _text.text = string.Empty;
            _text.maxVisibleCharacters = 0;
        }

        if (_canvas != null)
        {
            _canvas.enabled = false;
        }

        _displayCompletion?.TrySetResult(true);
        _displayCompletion = null;
    }

    public Task ShowForDurationAsync(string message, float secondsPerCharacter)
    {
        if (_canvas == null || _text == null)
        {
            Debug.LogWarning("[DialogController] Missing canvas or text reference. Unable to show dialog.");
            return Task.CompletedTask;
        }

        var messageToShow = message ?? string.Empty;
        var resolvedSecondsPerCharacter = ResolveSecondsPerCharacter(secondsPerCharacter);

        Show(messageToShow, resolvedSecondsPerCharacter);

        _displayCompletion = new TaskCompletionSource<bool>();
        var displayDuration = Mathf.Max(0f, CalculateTypingDuration(messageToShow, resolvedSecondsPerCharacter));

        if (resolvedSecondsPerCharacter > 0f && !Mathf.Approximately(displayDuration, 0f))
        {
            displayDuration += resolvedSecondsPerCharacter;
        }

        _displayRoutine = StartCoroutine(DisplayRoutine(displayDuration));

        if (_displayRoutine == null)
        {
            _displayCompletion.TrySetResult(true);
        }

        return _displayCompletion.Task;
    }

    private IEnumerator TypeText(string message, float secondsPerCharacter)
    {
        _text.text = message;

        if (secondsPerCharacter <= 0f)
        {
            _text.maxVisibleCharacters = message.Length;
            _typingRoutine = null;
            yield break;
        }

        var delay = secondsPerCharacter;
        _text.maxVisibleCharacters = 0;

        for (var i = 1; i <= message.Length; i++)
        {
            _text.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }

        _text.maxVisibleCharacters = message.Length;
        _typingRoutine = null;
    }

    private IEnumerator DisplayRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        _displayRoutine = null;
        Hide();
    }

    private float CalculateTypingDuration(string message, float secondsPerCharacter)
    {
        if (secondsPerCharacter <= 0f || string.IsNullOrEmpty(message))
        {
            return 0f;
        }

        return message.Length * secondsPerCharacter;
    }

    private float ResolveSecondsPerCharacter(float overrideSecondsPerCharacter)
    {
        if (overrideSecondsPerCharacter >= 0f)
        {
            return overrideSecondsPerCharacter;
        }

        return _defaultSecondsPerCharacter;
    }
}
