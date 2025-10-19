using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public sealed class DialogController : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField, Min(1e-3f)] private float _charactersPerSecond = 24f;

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

        _canvas.enabled = true;
        _typingRoutine = StartCoroutine(TypeText(message ?? string.Empty));
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

    public Task ShowForDurationAsync(string message, float duration)
    {
        if (_canvas == null || _text == null)
        {
            Debug.LogWarning("[DialogController] Missing canvas or text reference. Unable to show dialog.");
            return Task.CompletedTask;
        }

        if (duration <= 0f)
        {
            Show(message);
            Hide();
            return Task.CompletedTask;
        }

        Show(message);

        _displayCompletion = new TaskCompletionSource<bool>();
        _displayRoutine = StartCoroutine(DisplayRoutine(duration));

        if (_displayRoutine == null)
        {
            _displayCompletion.TrySetResult(true);
        }

        return _displayCompletion.Task;
    }

    private IEnumerator TypeText(string message)
    {
        _text.text = message;

        if (_charactersPerSecond <= 0f)
        {
            _text.maxVisibleCharacters = message.Length;
            yield break;
        }

        var delay = 1f / _charactersPerSecond;
        _text.maxVisibleCharacters = 0;

        for (var i = 1; i <= message.Length; i++)
        {
            _text.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }

        _typingRoutine = null;
    }

    private IEnumerator DisplayRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        _displayRoutine = null;
        Hide();
    }
}
