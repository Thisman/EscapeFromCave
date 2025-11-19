using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public sealed class DialogManager : MonoBehaviour
{
    [SerializeField] private DungeonSceneUIController _uiController;
    [SerializeField, Min(0f)] private float _defaultSecondsPerCharacter = 0.05f;
    [SerializeField, Min(0f)] private float _typingStartDelaySeconds = 0.1f;
    [SerializeField, Min(0f)] private float _typingEndDelaySeconds = 0.5f;
    [SerializeField, Min(0f)] private float _delayBetweenShow = 0f;

    private Coroutine _displayRoutine;
    private TaskCompletionSource<bool> _displayCompletion;
    private float _activeSecondsPerCharacter;

    private void Awake()
    {
        Hide();
    }

    public void Show(string message)
    {
        if (!TryPrepareUiController(nameof(Show)))
        {
            return;
        }

        StopDisplayRoutine();
        CompleteDisplay(_displayCompletion);

        _activeSecondsPerCharacter = ResolveSecondsPerCharacter(_defaultSecondsPerCharacter);

        RenderDialog(message);
    }

    public void Hide()
    {
        HideInternal(_displayCompletion);
    }

    public Task ShowForDurationAsync(string message)
    {
        if (!TryPrepareUiController(nameof(ShowForDurationAsync)))
        {
            return Task.CompletedTask;
        }

        StopDisplayRoutine();
        CompleteDisplay(_displayCompletion);

        var messageToShow = message ?? string.Empty;
        _activeSecondsPerCharacter = ResolveSecondsPerCharacter(_defaultSecondsPerCharacter);

        var completion = new TaskCompletionSource<bool>();
        _displayCompletion = completion;

        _displayRoutine = StartCoroutine(DisplayRoutine(messageToShow, _activeSecondsPerCharacter, completion));

        if (_displayRoutine == null)
        {
            CompleteDisplay(completion);
        }

        return completion.Task;
    }

    private IEnumerator DisplayRoutine(string message, float secondsPerCharacter, TaskCompletionSource<bool> completion)
    {
        if (_typingStartDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(_typingStartDelaySeconds);
        }

        RenderDialog(message);

        var typingDuration = Mathf.Max(0f, CalculateTypingDuration(message, secondsPerCharacter));

        if (secondsPerCharacter > 0f && !Mathf.Approximately(typingDuration, 0f))
        {
            typingDuration += secondsPerCharacter;
        }

        if (!Mathf.Approximately(typingDuration, 0f))
        {
            yield return new WaitForSeconds(typingDuration);
        }

        if (_typingEndDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(_typingEndDelaySeconds);
        }

        if (_delayBetweenShow > 0f)
        {
            yield return new WaitForSeconds(_delayBetweenShow);
        }

        if (!ReferenceEquals(_displayCompletion, completion))
        {
            completion?.TrySetResult(true);
            yield break;
        }

        _displayRoutine = null;
        HideInternal(completion);
    }

    private void RenderDialog(string message)
    {
        var messageToShow = message ?? string.Empty;
        _uiController.RenderDialog(messageToShow, _activeSecondsPerCharacter);
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

        if (_uiController != null)
        {
            return _uiController.DialogSecondsPerCharacter;
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

        StopDisplayRoutine();

        if (_uiController != null)
        {
            _uiController.HideDialog();
        }

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

    private void StopDisplayRoutine()
    {
        if (_displayRoutine != null)
        {
            StopCoroutine(_displayRoutine);
            _displayRoutine = null;
        }
    }

    private bool TryPrepareUiController(string caller)
    {
        if (_uiController != null)
        {
            return true;
        }

        Debug.LogWarning($"[{nameof(DialogManager)}.{caller}] Missing UI controller. Unable to show dialog.");
        return false;
    }
}
