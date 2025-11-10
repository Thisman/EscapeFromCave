public sealed class DefaultCursorSource : ICursorSource
{
    public override CursorSourceData GetCursorState()
    {
        return new CursorSourceData
        {
            Cursor = CursorOnHover
        };
    }
}
