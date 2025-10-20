using System.Collections.Generic;
using UnityEngine;

public sealed class ArmyRoasterView : MonoBehaviour
{
    public PlayerArmyController PlayerArmyController; 
    
    [SerializeField] private RectTransform _content;
    [SerializeField] private ArmyRoasterSquadView _itemPrefab;

    private readonly List<ArmyRoasterSquadView> _items = new();

    private void Awake()
    {
        if (_content == null)
            _content = (RectTransform)transform;
    }

    private void OnEnable()
    {
        PlayerArmyController.ArmyChanged += OnArmyChanged;
    }

    private void OnDisable()
    {
        PlayerArmyController.ArmyChanged -= OnArmyChanged;
    }

    private void OnArmyChanged(IReadOnlyArmyModel army)
    {
        Rebuild();
    }

    public void Rebuild()
    {
        if (PlayerArmyController == null)
        {
            Debug.LogWarning("[ArmyPanelUI] PlayerArmyController не назначен");
            ClearAll();
            return;
        }

        var squads = PlayerArmyController.GetSquads();
        int count = squads.Count;
        EnsureCapacity(count);

        for (int i = 0; i < count; i++)
        {
            var view = _items[i];
            view.gameObject.SetActive(true);
            view.Bind(squads[i]);
        }

        for (int i = count; i < _items.Count; i++)
            _items[i].gameObject.SetActive(false);
    }

    public void RefreshValues()
    {
        if (PlayerArmyController == null) return;

        var squads = PlayerArmyController.GetSquads();
        int visible = Mathf.Min(squads.Count, _items.Count);
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
