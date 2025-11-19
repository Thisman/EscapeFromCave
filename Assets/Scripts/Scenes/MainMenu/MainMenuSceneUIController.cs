using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuSceneUIController : MonoBehaviour, ISceneUIController
{
    [SerializeField] private UIDocument _uiDocument;

    private Button _startButton;
    private bool _isStartRequested;

    private bool _isAttached;

    public Func<Task> OnStartGame;

    private void Awake()
    {
        TryRegisterLifecycleCallbacks();
    }

    private void OnEnable()
    {
        TryRegisterLifecycleCallbacks();
    }

    private void OnDestroy()
    {
        DetachFromPanel();

        if (_uiDocument?.rootVisualElement is { } root)
        {
            root.UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
        }
    }

    public void AttachToPanel(UIDocument document)
    {
        if (document == null)
        {
            return;
        }

        if (_isAttached)
        {
            DetachFromPanel();
        }

        _uiDocument = document;

        var root = document.rootVisualElement;
        if (root == null)
        {
            return;
        }

        _startButton = root.Q<Button>("StartGameButton");
        if (_startButton != null)
        {
            _startButton.clicked += HandleStartButtonClicked;
        }

        _isAttached = true;
    }

    public void DetachFromPanel()
    {
        if (!_isAttached)
        {
            return;
        }

        if (_startButton != null)
        {
            _startButton.clicked -= HandleStartButtonClicked;
            _startButton = null;
        }

        _isAttached = false;
    }

    private void HandleStartButtonClicked()
    {
        _ = RequestStartGameAsync();
    }

    private async Task RequestStartGameAsync()
    {
        if (_isStartRequested)
        {
            Debug.Log("Start game request is already running", this);
            return;
        }

        if (_startButton != null)
        {
            _startButton.SetEnabled(false);
        }

        _isStartRequested = true;

        try
        {
            if (OnStartGame != null)
            {
                Debug.Log("[MainMenuUI] Starting game scene transition", this);
                await OnStartGame.Invoke();
                Debug.Log("[MainMenuUI] Game scene transition finished", this);
            }
            else
            {
                Debug.LogWarning("OnStartGame handler is not assigned", this);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to start game: {exception}", this);
        }
        finally
        {
            _isStartRequested = false;

            if (_startButton != null)
            {
                _startButton.SetEnabled(true);
            }
        }
    }

    private void TryRegisterLifecycleCallbacks()
    {
        if (_uiDocument == null)
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        if (_uiDocument?.rootVisualElement is { } root)
        {
            root.UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            root.RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);

            if (!_isAttached && root.panel != null)
            {
                AttachToPanel(_uiDocument);
            }
        }
    }

    private void HandleAttachToPanel(AttachToPanelEvent _)
    {
        if (!_isAttached)
        {
            AttachToPanel(_uiDocument);
        }
    }

    private void HandleDetachFromPanel(DetachFromPanelEvent _)
    {
        DetachFromPanel();
    }
}
