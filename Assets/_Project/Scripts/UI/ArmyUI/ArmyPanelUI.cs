using System.Collections.Generic;
using UnityEngine;
using VContainer;

public sealed class ArmyPanelUI : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private PlayerArmyController _playerArmyController;
    [SerializeField] private RectTransform _content;
    [SerializeField] private SquadItemView _itemPrefab;

    [Inject] private GameFlowService _gameFlowService;

    private readonly List<SquadItemView> _items = new();
    private ArmyPanelPresenter _presenter;
    private bool _flowSubscribed;

    private void Awake()
    {
        if (_content == null)
            _content = (RectTransform)transform;

        _presenter = new ArmyPanelPresenter(_playerArmyController);
        _presenter.Updated += OnPresenterUpdated;
    }

    private void OnEnable()
    {
        SubscribeToFlow();
        _presenter?.RequestRefresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromFlow();
    }

    private void OnDestroy()
    {
        if (_presenter != null)
        {
            _presenter.Updated -= OnPresenterUpdated;
            _presenter.Dispose();
            _presenter = null;
        }
    }

    private void OnPresenterUpdated(IReadOnlyList<SquadViewModel> squads)
    {
        if (_playerArmyController == null)
        {
            Debug.LogWarning("[ArmyPanelUI] PlayerArmyController не назначен");
            ClearAll();
            return;
        }

        ApplyViewModels(squads);
    }

    private void ApplyViewModels(IReadOnlyList<SquadViewModel> squads)
    {
        int count = squads?.Count ?? 0;
        EnsureCapacity(count);

        int i = 0;
        for (; i < count; i++)
        {
            var view = _items[i];
            view.gameObject.SetActive(true);
            view.Bind(squads[i]);
        }

        for (; i < _items.Count; i++)
            _items[i].gameObject.SetActive(false);
    }

    private void EnsureCapacity(int needed)
    {
        while (_items.Count < needed)
        {
            var view = Instantiate(_itemPrefab, _content);
            view.gameObject.SetActive(false);
            _items.Add(view);
        }
    }

    private void ClearAll()
    {
        for (int i = 0; i < _items.Count; i++)
            _items[i].gameObject.SetActive(false);
    }

    private void SubscribeToFlow()
    {
        if (_gameFlowService == null || _flowSubscribed)
            return;

        _gameFlowService.ModeChanged += HandleModeChanged;
        _flowSubscribed = true;
    }

    private void UnsubscribeFromFlow()
    {
        if (!_flowSubscribed || _gameFlowService == null)
            return;

        _gameFlowService.ModeChanged -= HandleModeChanged;
        _flowSubscribed = false;
    }

    private void HandleModeChanged(GameMode mode)
    {
        bool shouldBeVisible = mode == GameMode.Inventory || mode == GameMode.Gameplay;
        if (_content != null && _content.gameObject != gameObject)
            _content.gameObject.SetActive(shouldBeVisible);

        if (shouldBeVisible)
            _presenter?.RequestRefresh();
    }
}
