using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class PlayerLightController : MonoBehaviour
{
    [SerializeField] private Light2D _light;
    [Header("Heartbeat settings")]
    [SerializeField, Min(0f)] private float _beatsPerMinute = 60f;
    [SerializeField] private float _radiusOffset = 0.5f;
    [SerializeField] private AnimationCurve _heartbeatCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.08f, 1f, 0f, -6f),
        new Keyframe(0.16f, 0.2f, 0f, 0f),
        new Keyframe(0.3f, 0.8f, 0f, -4f),
        new Keyframe(0.6f, 0f, 0f, 0f),
        new Keyframe(1f, 0f, 0f, 0f));

    private float _baseInnerRadius;
    private float _baseOuterRadius;
    private bool _hasCachedBaseValues;

    private void Awake()
    {
        CaptureBaseValues();
    }

    private void OnEnable()
    {
        if (!_hasCachedBaseValues)
        {
            CaptureBaseValues();
        }
    }

    private void Reset()
    {
        CaptureBaseValues(true);
    }

    private void CaptureBaseValues(bool force = false)
    {
        if (_light == null)
        {
            return;
        }

        if (_hasCachedBaseValues && !force)
        {
            return;
        }

        _baseInnerRadius = _light.pointLightInnerRadius;
        _baseOuterRadius = _light.pointLightOuterRadius;
        _hasCachedBaseValues = true;
    }

    private void Update()
    {
        if (_light == null || _beatsPerMinute <= 0f)
        {
            return;
        }

        float beatsPerSecond = _beatsPerMinute / 60f;
        float phase = Mathf.Repeat(Time.time * beatsPerSecond, 1f);
        float pulse = Mathf.Clamp01(_heartbeatCurve.Evaluate(phase));

        ApplyPulse(pulse);
    }

    private void ApplyPulse(float pulse)
    {
        float offset = _radiusOffset * pulse;
        _light.pointLightInnerRadius = _baseInnerRadius + offset;
        _light.pointLightOuterRadius = _baseOuterRadius + offset;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            _hasCachedBaseValues = false;
            CaptureBaseValues(true);
            if (_light != null)
            {
                float pulse = _beatsPerMinute > 0f ? Mathf.Clamp01(_heartbeatCurve.Evaluate(0f)) : 0f;
                ApplyPulse(pulse);
            }
        }
    }
#endif

    public void SetBeatsPerMinute(float beatsPerMinute)
    {
        _beatsPerMinute = Mathf.Max(0f, beatsPerMinute);
    }

    public void SetRadiusOffset(float radiusOffset)
    {
        _radiusOffset = Mathf.Max(0f, radiusOffset);
    }
}
