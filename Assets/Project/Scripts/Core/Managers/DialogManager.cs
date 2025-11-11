using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class DialogManager : MonoBehaviour
{
    private const string DialogContainerName = "DialogContainer";
    private const string DialogLabelName = "DialogLabel";

    [SerializeField] private UIDocument _document;
    [SerializeField, Min(0f)] private float _defaultSecondsPerCharacter = 0.05f;
    [SerializeField, Min(0f)] private float _delayBetweenShow = 0f;

    private VisualElement _dialogContainer;
    private Label _dialogLabel;
    private Coroutine _typingRoutine;
    private Coroutine _displayRoutine;
    private TaskCompletionSource<bool> _displayCompletion;

    private void Awake() => TryResolveDocument();

    public void Show(string message)
    {
        Show(message, _defaultSecondsPerCharacter);
    }

    public void Show(string message, float secondsPerCharacter)
    {
        if (!EnsureDialogElements())
            return;

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

        var messageToShow = message ?? string.Empty;
        var resolvedSecondsPerCharacter = ResolveSecondsPerCharacter(secondsPerCharacter);

        _dialogContainer.style.display = DisplayStyle.Flex;
        _typingRoutine = StartCoroutine(TypeText(messageToShow, resolvedSecondsPerCharacter));
    }

    public void Hide()
    {
        HideInternal(_displayCompletion);
    }

    public Task ShowForDurationAsync(string message, float secondsPerCharacter)
    {
        if (!EnsureDialogElements())
            return Task.CompletedTask;

        var messageToShow = message ?? string.Empty;
        var resolvedSecondsPerCharacter = ResolveSecondsPerCharacter(secondsPerCharacter);

        Show(messageToShow, resolvedSecondsPerCharacter);

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
        if (_dialogLabel == null)
        {
            _typingRoutine = null;
            yield break;
        }

        if (secondsPerCharacter <= 0f)
        {
            _dialogLabel.text = message;
            _typingRoutine = null;
            yield break;
        }

        var delay = secondsPerCharacter;
        var builder = new StringBuilder(message.Length);

        _dialogLabel.text = string.Empty;

        foreach (char character in message)
        {
            builder.Append(character);
            _dialogLabel.text = builder.ToString();
            yield return new WaitForSeconds(delay);
        }

        _dialogLabel.text = message;
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

    private bool EnsureDialogElements()
    {
        if (_dialogContainer != null && _dialogLabel != null)
            return true;

        if (!TryResolveDocument())
        {
            Debug.LogWarning($"[{nameof(DialogManager)}] Missing UIDocument reference. Unable to locate dialog container.");
            return false;
        }

        var root = _document.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning($"[{nameof(DialogManager)}] UIDocument root visual element is not ready. Unable to locate dialog container.");
            return false;
        }

        _dialogContainer = root.Q<VisualElement>(DialogContainerName);

        if (_dialogContainer == null)
        {
            Debug.LogWarning($"[{nameof(DialogManager)}] '{DialogContainerName}' element not found in the UI document.");
            return false;
        }

        _dialogLabel = _dialogContainer.Q<Label>(DialogLabelName);

        if (_dialogLabel == null)
        {
            _dialogLabel = new Label
            {
                name = DialogLabelName
            };

            _dialogContainer.Add(_dialogLabel);
        }

        _dialogContainer.style.display = DisplayStyle.None;
        _dialogLabel.text = string.Empty;

        return true;
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

        if (_dialogContainer != null)
        {
            _dialogContainer.style.display = DisplayStyle.None;
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

    private bool TryResolveDocument()
    {
        if (_document != null)
            return true;

        _document = GetComponent<UIDocument>();

        if (_document != null)
            return true;

        _document = FindObjectOfType<UIDocument>(true);
        return _document != null;
    }
}
