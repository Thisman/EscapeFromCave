using UnityEngine;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public static class GameLogger
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, [CallerMemberName] string method = "") {
        UnityEngine.Debug.Log($"[{method}] {message}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Warn(string message, [CallerMemberName] string method = "") {
        UnityEngine.Debug.LogWarning($"[WARN][{method}] {message}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Error(string message,  [CallerMemberName] string method = "") {
        UnityEngine.Debug.LogError($"[ERROR][{method}] {message}");
    }
}
