using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractionHintController : MonoBehaviour
{
    [SerializeField] private GameObject _hintPrefab;
    [SerializeField] private Vector3 _worldOffset = new(0f, 1f, 0f);

    private GameObject _hintInstance;
    private HintAnimationController _animation;
    private bool _isVisible;

    public void ShowHint()
    {
        if (!TryEnsureInstance())
            return;

        _isVisible = true;
        _hintInstance.SetActive(true);
        UpdateHintTransform();
    }

    public void HideHint()
    {
        if (!_hintInstance || !_isVisible)
            return;

        _isVisible = false;
        _hintInstance.SetActive(false);
    }

    private bool TryEnsureInstance()
    {
        if (_hintInstance)
            return true;

        if (!_hintPrefab)
            return false;

        _hintInstance = Instantiate(_hintPrefab, transform);
        _animation = _hintInstance.GetComponent<HintAnimationController>();
        _hintInstance.SetActive(false);
        UpdateHintTransform();
        return true;
    }

    private void LateUpdate()
    {
        if (_isVisible)
            UpdateHintTransform();
    }

    private void UpdateHintTransform()
    {
        if (!_hintInstance)
            return;

        Vector3 targetPosition = transform.position + _worldOffset;
        if (_animation)
            _animation.SetBasePosition(targetPosition);
        else
            _hintInstance.transform.position = targetPosition;
    }
}
