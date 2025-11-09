using UnityEngine;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public static class GameLogger
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message,
        [CallerMemberName] string method = "")
    {
        var callerType = new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name;
        UnityEngine.Debug.Log($"[{callerType}.{method}] {message}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Warn(string message,
        [CallerMemberName] string method = "")
    {
        var callerType = new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name;
        UnityEngine.Debug.LogWarning($"[WARN][{callerType}.{method}] {message}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Error(string message,
        [CallerMemberName] string method = "")
    {
        var callerType = new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name;
        UnityEngine.Debug.LogError($"[ERROR][{callerType}.{method}] {message}");
    }
}
