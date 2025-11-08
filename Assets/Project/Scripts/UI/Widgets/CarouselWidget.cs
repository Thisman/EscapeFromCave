using UnityEngine;
using UnityEngine.UI;

public class CarouselWidget : MonoBehaviour
{
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private RectTransform _content;

    [SerializeField] private bool _loop = false;

    private int _currentIndex = 0;
    private RectTransform[] _items = System.Array.Empty<RectTransform>();

    private void Awake()
    {
        RefreshItems();

        _prevButton.onClick.AddListener(Prev);
        _nextButton.onClick.AddListener(Next);
    }

    private void OnDestroy()
    {
        _prevButton.onClick.RemoveListener(Prev);
        _nextButton.onClick.RemoveListener(Next);
    }

    public GameObject GetCurrentObject()
    {
        if (_items == null || _items.Length == 0)
            return null;

        return _items[_currentIndex]?.gameObject;
    }

    public RectTransform Content => _content;

    public void RefreshItems()
    {
        int count = _content.childCount;
        _items = new RectTransform[count];
        for (int i = 0; i < count; i++)
            _items[i] = _content.GetChild(i) as RectTransform;

        ShowIndex(_items.Length == 0 ? 0 : Mathf.Clamp(_currentIndex, 0, _items.Length - 1));
    }

    private void Prev()
    {
        int newIndex = _currentIndex - 1;
        if (newIndex < 0)
            newIndex = _loop ? _items.Length - 1 : 0;

        ShowIndex(newIndex);
    }

    private void Next()
    {
        int newIndex = _currentIndex + 1;
        if (newIndex >= _items.Length)
            newIndex = _loop ? 0 : _items.Length - 1;

        ShowIndex(newIndex);
    }

    private void ShowIndex(int index)
    {
        if (_items == null || _items.Length == 0)
        {
            _currentIndex = 0;
            return;
        }

        _currentIndex = Mathf.Clamp(index, 0, _items.Length - 1);

        for (int i = 0; i < _items.Length; i++)
            _items[i].gameObject.SetActive(i == _currentIndex);
    }
}
