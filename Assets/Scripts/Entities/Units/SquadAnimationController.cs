using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SquadController))]
public class SquadAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SquadController _unitController;
    [SerializeField] private float _scaleAmplitude = 0.03f;
    [SerializeField] private float _scaleFrequency = 6f;

    private Vector3 _initialScale;
    private float _scaleAnimationStartTime;

    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        _initialScale = transform.localScale;
        ScheduleScaleAnimation();
    }

    private void Start()
    {
        var model = _unitController.GetSquadModel();
        if (model != null)
            _spriteRenderer.sprite = model.Icon;
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

        float elapsed = Time.time - _scaleAnimationStartTime;
        if (elapsed < 0f)
        {
            transform.localScale = _initialScale;
            return;
        }

        var scaleOffset = Mathf.Sin(elapsed * _scaleFrequency) * _scaleAmplitude;
        var targetScale = _initialScale;
        targetScale.y = _initialScale.y * (1f + scaleOffset);
        transform.localScale = targetScale;
    }

    private void ScheduleScaleAnimation()
    {
        _scaleAnimationStartTime = Time.time + UnityEngine.Random.Range(0f, 0.5f);
    }
}
