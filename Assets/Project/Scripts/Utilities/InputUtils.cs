using UnityEngine;
using UnityEngine.InputSystem;

public static class InputUtils
{
    public static bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
        // 1. Мышь (настольный кейс)
        if (Mouse.current is { } mouse && mouse.added)
        {
            screenPosition = mouse.position.ReadValue();
            return true;
        }

        // 2. Сенсорный экран (мобильные устройства)
        if (Touchscreen.current is { } touchScreen && touchScreen.touches.Count > 0)
        {
            var touch = touchScreen.primaryTouch;
            if (touch.press.isPressed)
            {
                screenPosition = touch.position.ReadValue();
                return true;
            }
        }

        // 3. Перьевой ввод (пен)
        if (Pen.current is { } pen && pen.added)
        {
            screenPosition = pen.position.ReadValue();
            return true;
        }

        // 4. Любой другой Pointer (на случай кастомных устройств)
        if (Pointer.current is { } pointer && pointer.added)
        {
            screenPosition = pointer.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    public static Vector2 PanelToScreenPosition(Vector2 panelPosition)
    {
        return new Vector2(panelPosition.x, Screen.height - panelPosition.y);
    }
}
