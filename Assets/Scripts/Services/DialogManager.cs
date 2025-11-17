using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public sealed class DialogManager : MonoBehaviour
{
    [SerializeField] private DungeonUIController _uiController;
    [SerializeField, Min(0f)] private float _defaultSecondsPerCharacter = 0.05f;
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
        if (_uiController == null)
        {
            Debug.LogWarning($"[{nameof(DialogManager)}.{nameof(Show)}] Missing UI controller. Unable to show dialog.");
            return;
        }

        if (_displayRoutine != null)
        {
            StopCoroutine(_displayRoutine);
            _displayRoutine = null;
        }

        CompleteDisplay(_displayCompletion);

        var messageToShow = message ?? string.Empty;
        _activeSecondsPerCharacter = ResolveSecondsPerCharacter(_defaultSecondsPerCharacter);

        _uiController.RenderDialog(messageToShow, _activeSecondsPerCharacter);
    }

    public void Hide()
    {
        HideInternal(_displayCompletion);
    }

    public Task ShowForDurationAsync(string message)
    {
        if (_uiController == null)
        {
            Debug.LogWarning($"[{nameof(DialogManager)}.{nameof(ShowForDurationAsync)}] Missing UI controller. Unable to show dialog.");
            return Task.CompletedTask;
        }

        var messageToShow = message ?? string.Empty;
        Show(messageToShow);

        var completion = new TaskCompletionSource<bool>();
        _displayCompletion = completion;

        var displayDuration = Mathf.Max(0f, CalculateTypingDuration(messageToShow, _activeSecondsPerCharacter));

        if (_activeSecondsPerCharacter > 0f && !Mathf.Approximately(displayDuration, 0f))
        {
            displayDuration += _activeSecondsPerCharacter;
        }

        displayDuration += _delayBetweenShow;

        _displayRoutine = StartCoroutine(DisplayRoutine(displayDuration, completion));

        if (_displayRoutine == null)
        {
            CompleteDisplay(completion);
        }

        return completion.Task;
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

        if (_displayRoutine != null)
        {
            StopCoroutine(_displayRoutine);
            _displayRoutine = null;
        }

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
}
