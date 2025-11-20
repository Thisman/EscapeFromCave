using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BaseUIController<E> : MonoBehaviour
{
    [SerializeField] protected UIDocument _uiDocument;

    protected bool _isAttached;
    protected bool _initialized;
    protected GameEventBusService _sceneEventBusService;
    protected readonly Dictionary<E, object> _uiElements = new();

    protected void Awake()
    {
        TryRegisterLifecycleCallbacks();
    }

    protected void OnEnable()
    {
        TryRegisterLifecycleCallbacks();
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
        _sceneEventBusService = gameEventBusService;

        SubscriveToGameEvents();
        _initialized = true;
    }

    protected T GetElement<T>(E id) where T : class
    {
        return _uiElements[id] as T;
    }

    protected void AttachToPanel(UIDocument document)
    {
        if (_isAttached)
            return;

        RegisterUIElements();
        SubcribeToUIEvents();

        _isAttached = true;
    }

    protected void DetachFromPanel()
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

    protected void TryUnregisterLifecycleCallbacks()
    {
        if (_uiDocument.rootVisualElement is { } root)
        {
            root.UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
        }
    }

    virtual protected void RegisterUIElements() { }

    virtual protected void SubcribeToUIEvents() { }

    virtual protected void UnsubscriveFromUIEvents() { }

    virtual protected void SubscriveToGameEvents() { }

    virtual protected void UnsubscribeFromGameEvents() { }

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
