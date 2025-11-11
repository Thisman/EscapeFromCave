using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class DialogManager : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private DangeonLevelSceneUIController _dangeonLevelSceneUIController;
    [SerializeField, Min(0f)] private float _defaultSecondsPerCharacter = 0.05f;
    [SerializeField, Min(0f)] private float _delayBetweenShow = 0f;

    private Coroutine _typingRoutine;
    private Coroutine _displayRoutine;
    private TaskCompletionSource<bool> _displayCompletion;
    private Label _dialogLabel;

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

        ResolveUIController()?.HideDialog();
    }

    public void Show(string message)
    {
        Show(message, _defaultSecondsPerCharacter);
    }

    public void Show(string message, float secondsPerCharacter)
    {
        if (!TrySetupShow(message, secondsPerCharacter, out var messageToShow, out var resolvedSecondsPerCharacter))
        {
            Debug.LogWarning($"[{nameof(DialogManager)}.{nameof(Show)}] Missing dialog UI references. Unable to show dialog.");
            return;
        }

        _typingRoutine = StartCoroutine(TypeText(messageToShow, resolvedSecondsPerCharacter));
    }

    public void Hide()
    {
        HideInternal(_displayCompletion);
    }

    public Task ShowForDurationAsync(string message, float secondsPerCharacter)
    {
        if (!TrySetupShow(message, secondsPerCharacter, out var messageToShow, out var resolvedSecondsPerCharacter))
        {
            Debug.LogWarning($"[{nameof(DialogManager)}.{nameof(ShowForDurationAsync)}] Missing dialog UI references. Unable to show dialog.");
            return Task.CompletedTask;
        }

        _typingRoutine = StartCoroutine(TypeText(messageToShow, resolvedSecondsPerCharacter));

        var completion = new TaskCompletionSource<bool>();
        _displayCompletion = completion;

        var displayDuration = Mathf.Max(0f, CalculateTypingDuration(messageToShow, resolvedSecondsPerCharacter));

        if (resolvedSecondsPerCharacter > 0f && !Mathf.Approximately(displayDuration, 0f))
        {
            displayDuration += resolvedSecondsPerCharacter;
        }

        displayDuration += _delayBetweenShow;

        _displayRoutine = StartCoroutine(DisplayRoutine(displayDuration, completion));

        if (_displayRoutine == null)
        {
            CompleteDisplay(completion);
        }

        return completion.Task;
    }

    private IEnumerator TypeText(string message, float secondsPerCharacter)
    {
        if (_dialogLabel != null)
        {
            if (secondsPerCharacter <= 0f)
            {
                _dialogLabel.text = message;
                _typingRoutine = null;
                yield break;
            }

            var delay = secondsPerCharacter;
            _dialogLabel.text = string.Empty;

            for (var i = 1; i <= message.Length; i++)
            {
                _dialogLabel.text = message.Substring(0, i);
                yield return new WaitForSeconds(delay);
            }

            _dialogLabel.text = message;
            _typingRoutine = null;
            yield break;
        }

        if (_text == null)
        {
            _typingRoutine = null;
            yield break;
        }

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

    private IEnumerator DisplayRoutine(float duration, TaskCompletionSource<bool> completion)
    {
        yield return new WaitForSeconds(duration);

        if (!ReferenceEquals(_displayCompletion, completion))
        {
            completion?.TrySetResult(true);
            yield break;
        }

        _displayRoutine = null;
        HideInternal(completion);
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

    private void HideInternal(TaskCompletionSource<bool> completion)
    {
        if (!ReferenceEquals(_displayCompletion, completion) && completion != null)
        {
            completion.TrySetResult(true);
            return;
        }

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

        if (_dialogLabel != null)
        {
            _dialogLabel.text = string.Empty;
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

        ResolveUIController()?.HideDialog();

        CompleteDisplay(completion);
    }

    private void CompleteDisplay(TaskCompletionSource<bool> completion)
    {
        if (completion == null)
        {
            return;
        }

        completion.TrySetResult(true);

        if (ReferenceEquals(_displayCompletion, completion))
        {
            _displayCompletion = null;
        }
    }

    private bool TrySetupShow(string message, float secondsPerCharacter, out string messageToShow, out float resolvedSecondsPerCharacter)
    {
        messageToShow = string.Empty;
        resolvedSecondsPerCharacter = 0f;

        if (!PrepareDialogTarget())
            return false;

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

        CompleteDisplay(_displayCompletion);

        messageToShow = message ?? string.Empty;
        resolvedSecondsPerCharacter = ResolveSecondsPerCharacter(secondsPerCharacter);
        return true;
    }

    private bool PrepareDialogTarget()
    {
        var controller = ResolveUIController();
        if (controller != null)
        {
            var label = controller.ShowDialog();
            if (label != null)
            {
                _dialogLabel = label;
                _dialogLabel.text = string.Empty;

                if (_canvas != null)
                    _canvas.enabled = false;

                if (_text != null)
                {
                    _text.text = string.Empty;
                    _text.maxVisibleCharacters = 0;
                }

                return true;
            }
        }

        if (_canvas != null && _text != null)
        {
            _canvas.enabled = true;
            _text.text = string.Empty;
            _text.maxVisibleCharacters = 0;
            _dialogLabel = null;
            return true;
        }

        return false;
    }

    private DangeonLevelSceneUIController ResolveUIController()
    {
        if (_dangeonLevelSceneUIController != null)
            return _dangeonLevelSceneUIController;

        _dangeonLevelSceneUIController = FindObjectOfType<DangeonLevelSceneUIController>();
        return _dangeonLevelSceneUIController;
    }
}
