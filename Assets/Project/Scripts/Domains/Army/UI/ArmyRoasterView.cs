using System.Collections.Generic;
using UnityEngine;

public sealed class ArmyRoasterView : MonoBehaviour
{
    [SerializeField] private RectTransform _content;
    [SerializeField] private ArmyRoasterSquadView _itemPrefab;

    private PlayerArmyController _playerArmyController;
    private readonly List<ArmyRoasterSquadView> _items = new();
    private readonly List<IReadOnlySquadModel> _visibleSquads = new();

    private void Awake()
    {
        if (_content == null)
            _content = (RectTransform)transform;
    }

    private void OnEnable()
    {
        if (_playerArmyController)
            _playerArmyController.ArmyChanged += OnArmyChanged;
    }

    private void OnDisable()
    {
        if (_playerArmyController)
            _playerArmyController.ArmyChanged -= OnArmyChanged;
    }

    public void Render(PlayerArmyController playerArmyController)
    {
        _playerArmyController = playerArmyController;
        _playerArmyController.ArmyChanged += OnArmyChanged;
        Rebuild();
    }

    private void OnArmyChanged(IReadOnlyArmyModel army)
    {
        Rebuild();
    }

    public void Rebuild()
    {
        if (_playerArmyController == null)
        {
            Debug.LogWarning("[ArmyPanelUI] PlayerArmyController не назначен");
            ClearAll();
            return;
        }

        BuildVisibleSquadCache();

        int count = _visibleSquads.Count;
        EnsureCapacity(count);

        for (int i = 0; i < count; i++)
        {
            var view = _items[i];
            view.gameObject.SetActive(true);
            view.Render(_visibleSquads[i]);
        }

        for (int i = count; i < _items.Count; i++)
            _items[i].gameObject.SetActive(false);
    }

    public void RefreshValues()
    {
        if (_playerArmyController == null) return;

        BuildVisibleSquadCache();

        int visible = Mathf.Min(_visibleSquads.Count, _items.Count);
        for (int i = 0; i < visible; i++)
            if (_items[i].isActiveAndEnabled)
                _items[i].Render(_visibleSquads[i]);
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

    private void BuildVisibleSquadCache()
    {
        _visibleSquads.Clear();

        var squads = _playerArmyController.Army.GetSquads();
        for (int i = 0; i < squads.Count; i++)
        {
            var squad = squads[i];
            if (squad == null || squad.IsEmpty)
                continue;

            _visibleSquads.Add(squad);
        }
    }

    private void ClearAll()
    {
        for (int i = 0; i < _items.Count; i++)
            _items[i].gameObject.SetActive(false);
    }
}
