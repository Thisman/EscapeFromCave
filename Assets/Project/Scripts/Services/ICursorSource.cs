using UnityEngine;

public abstract class ICursorSource : MonoBehaviour
{
    public string CursorOnHover;

    public abstract CursorSourceData GetCursorState();
}
