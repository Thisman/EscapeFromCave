using UnityEngine;

public class UnitAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SquadController _unitController;
    [SerializeField] private float _scaleAmplitude = 0.1f;
    [SerializeField] private float _scaleFrequency = 2f;

    private Vector3 _initialScale;

    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void Start()
    {
        if (_spriteRenderer == null || _unitController == null)
            return;

        var model = _unitController.GetSquadModel();
        if (model != null)
            _spriteRenderer.sprite = model.Icon;
    }

    private void OnEnable()
    {
        _initialScale = transform.localScale;
    }

    private void OnDisable()
    {
        transform.localScale = _initialScale;
    }

    private void Update()
    {
        AnimateScale();
    }

    private void AnimateScale()
    {
        if (_scaleAmplitude <= 0f || _scaleFrequency <= 0f)
        {
            transform.localScale = _initialScale;
            return;
        }

        var scaleOffset = Mathf.Sin(Time.time * _scaleFrequency) * _scaleAmplitude;
        var targetScale = _initialScale;
        targetScale.y = _initialScale.y * (1f + scaleOffset);
        transform.localScale = targetScale;
    }
}
