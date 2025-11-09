using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

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

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Exception(Exception exception, [CallerMemberName] string method = "") {
        if (exception == null)
        {
            return;
        }

        UnityEngine.Debug.LogError($"[ERROR][{method}] {exception.Message}");
        UnityEngine.Debug.LogException(exception);
    }
}
