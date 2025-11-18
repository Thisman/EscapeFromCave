using UnityEngine;

public sealed class BattleSquadCursorSource : ICursorSource
{
    [SerializeField] private string _targetCursorName;
    [SerializeField] private string _defaultCursorName;

    private BattleSquadController _controller;

    private void Awake()
    {
        _controller ??= GetComponent<BattleSquadController>();
    }

    public override CursorSourceData GetCursorState()
    {
        return new CursorSourceData
        {
            Cursor = ResolveCursorName()
        };
    }

    private string ResolveCursorName()
    {
        var controller = _controller ??= GetComponent<BattleSquadController>();
        if (controller != null && controller.GetSquadModel().IsAlly())
            return ResolveDefaultCursor();

        if (controller != null && controller.IsValidTarget())
            return ResolveTargetCursor();

        return ResolveDefaultCursor();
    }

    private string ResolveTargetCursor()
    {
        if (!string.IsNullOrWhiteSpace(_targetCursorName))
            return _targetCursorName;

        return ResolveDefaultCursor();
    }

    private string ResolveDefaultCursor()
    {
        if (!string.IsNullOrWhiteSpace(_defaultCursorName))
            return _defaultCursorName;

        return CursorOnHover;
    }
}
