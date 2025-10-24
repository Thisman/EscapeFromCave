using System.Collections.Generic;
using UnityEngine;

public sealed class BattleQueueUIController : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private BattleQueueItemView _itemPrefab;

    private readonly List<BattleQueueItemView> _items = new();

    private void Awake()
    {
        if (_container == null)
            _container = transform;
    }

    public void Init(BattleQueueController battleQueueController)
    {
        Update(battleQueueController);
    }

    public void Update(BattleQueueController battleQueueController)
    {
        if (battleQueueController == null)
        {
            Debug.LogWarning("[BattleQueueUI] BattleQueueController is missing.");
            ClearItems();
            return;
        }

        var queue = battleQueueController.GetQueue();
        if (queue == null)
        {
            Debug.LogWarning("[BattleQueueUI] Queue data is missing.");
            ClearItems();
            return;
        }

        if (!EnsureCapacity(queue.Count))
        {
            ClearItems();
            return;
        }

        for (int i = 0; i < queue.Count; i++)
        {
            var view = _items[i];
            view.gameObject.SetActive(true);
            view.Bind(queue[i]);
        }

        for (int i = queue.Count; i < _items.Count; i++)
            _items[i].gameObject.SetActive(false);
    }

    private bool EnsureCapacity(int needed)
    {
        if (_itemPrefab == null)
        {
            Debug.LogWarning("[BattleQueueUI] Item prefab is not assigned.");
            return false;
        }

        while (_items.Count < needed)
        {
            var view = Instantiate(_itemPrefab, _container);
            view.gameObject.SetActive(false);
            _items.Add(view);
        }

        return _items.Count >= needed;
    }

    private void ClearItems()
    {
        for (int i = 0; i < _items.Count; i++)
            _items[i].gameObject.SetActive(false);
    }
}
