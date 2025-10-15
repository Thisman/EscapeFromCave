using System;

[Serializable]
public struct CooldownState
{
    private float _nextReadyTime;

    public bool Ready(float currentTime)
        => currentTime >= _nextReadyTime;

    public void Start(float currentTime, float cooldownDuration)
        => _nextReadyTime = currentTime + cooldownDuration;

    public float Remaining(float currentTime)
        => MathF.Max(0f, _nextReadyTime - currentTime);
}
