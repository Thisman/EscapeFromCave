using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

abstract public class BaseUIController<E> : MonoBehaviour
{
    [SerializeField] protected UIDocument _uiDocument;

    protected bool _isAttached;
    protected bool _initialized;
    protected GameEventBusService _sceneEventBusService;
    protected readonly Dictionary<E, object> _uiElements = new();

    protected void OnEnable()
    {
        TryRegisterLifecycleCallbacks();
        if (_initialized)
            SubscriveToGameEvents();
    }

    protected void OnDisable()
    {
        DetachFromPanel();
        TryUnregisterLifecycleCallbacks();
    }

    protected void OnDestroy()
    {
        DetachFromPanel();
        TryUnregisterLifecycleCallbacks();
    }

    virtual public void Initialize(GameEventBusService gameEventBusService) {
        if (_initialized)
        {
            Debug.LogWarning($"{GetType().Name} is already initialized.");
            return;
        }

        _sceneEventBusService = gameEventBusService;

        SubscriveToGameEvents();
        _initialized = true;
    }

    protected T GetElement<T>(E id) where T : class
    {
        return _uiElements[id] as T;
    }

    virtual protected void AttachToPanel(UIDocument document)
    {
        if (_isAttached)
            return;

        RegisterUIElements();
        SubcribeToUIEvents();

        _isAttached = true;
        Debug.Log($"{GetType().Name} attached to panel.");
    }

    virtual protected void DetachFromPanel()
    {
        if (!_isAttached)
            return;

        UnsubscriveFromUIEvents();
        UnsubscribeFromGameEvents();

        _uiElements.Clear();

        _isAttached = false;
    }

    protected void TryRegisterLifecycleCallbacks()
    {
        if (_uiDocument.rootVisualElement is { } root)
        {
            root.RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);

            if (!_isAttached && root.panel != null)
                AttachToPanel(_uiDocument);
        }
    }

    protected void TryUnregisterLifecycleCallbacks()
    {
        if (_uiDocument.rootVisualElement is { } root)
        {
            root.UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
        }
    }

    abstract protected void RegisterUIElements();

    abstract protected void SubcribeToUIEvents();

    abstract protected void UnsubscriveFromUIEvents();

    abstract protected void SubscriveToGameEvents();

    abstract protected void UnsubscribeFromGameEvents();

    protected void HandleAttachToPanel(AttachToPanelEvent _)
    {
        if (!_isAttached)
            AttachToPanel(_uiDocument);
    }

    protected void HandleDetachFromPanel(DetachFromPanelEvent _)
    {
        DetachFromPanel();
    }
}
