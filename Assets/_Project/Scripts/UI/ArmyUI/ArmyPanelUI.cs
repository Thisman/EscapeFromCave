using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ArmyPanelUI : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private PlayerArmyController _playerArmyController; 
    [SerializeField] private RectTransform _content;
    [SerializeField] private SquadItemView _itemPrefab;

    private readonly List<SquadItemView> _items = new();

    private void Awake()
    {
        if (_content == null)
            _content = (RectTransform)transform;
    }

    private void Start()
    {
        Rebuild();
    }

    private void OnEnable()
    {
        _playerArmyController.ArmyChanged += Rebuild;
    }

    private void OnDisable()
    {
        _playerArmyController.ArmyChanged -= Rebuild;
    }

    public void Rebuild()
    {
        if (_playerArmyController == null)
        {
            Debug.LogWarning("[ArmyPanelUI] PlayerArmyController не назначен");
            ClearAll();
            return;
        }

        var squads = _playerArmyController.GetSquads();
        EnsureCapacity(squads.Count());

        for (int i = 0; i < squads.Count(); i++)
        {
            var view = _items[i];
            view.gameObject.SetActive(true);
            view.Bind(squads[i]);
        }

        for (int i = squads.Count(); i < _items.Count; i++)
            _items[i].gameObject.SetActive(false);
    }

    public void RefreshValues()
    {
        if (_playerArmyController == null) return;

        var squads = _playerArmyController.GetSquads();
        int visible = Mathf.Min(squads.Count(), _items.Count);
        for (int i = 0; i < visible; i++)
            if (_items[i].isActiveAndEnabled)
                _items[i].Bind(squads[i]);
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
}
