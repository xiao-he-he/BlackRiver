using System.Collections.Generic;
using UnityEngine;

/// <summary>Independent pause owners cannot accidentally resume each other's gameplay.</summary>
public static class GamePauseState
{
    private static readonly HashSet<Object> owners = new HashSet<Object>();
    private static float previousTimeScale = 1f;
    private static bool previousClockPause;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        owners.Clear();
        previousTimeScale = 1f;
        previousClockPause = false;
    }

    public static void Acquire(Object owner)
    {
        if (owner == null || owners.Contains(owner)) return;
        if (owners.Count == 0)
        {
            previousTimeScale = Time.timeScale;
            previousClockPause = TimeManager.Instance != null && TimeManager.Instance.gameClockPause;
        }
        owners.Add(owner);
        EnsurePaused();
    }

    public static void EnsurePaused()
    {
        if (owners.Count == 0) return;
        Time.timeScale = 0f;
        if (TimeManager.Instance != null) TimeManager.Instance.gameClockPause = true;
    }

    public static void Release(Object owner)
    {
        if (!owners.Remove(owner)) return;
        if (owners.Count > 0)
        {
            EnsurePaused();
            return;
        }
        Time.timeScale = previousTimeScale;
        if (TimeManager.Instance != null) TimeManager.Instance.gameClockPause = previousClockPause;
    }

    public static void ResetForSceneChange()
    {
        owners.Clear();
        Time.timeScale = 1f;
        if (TimeManager.Instance != null) TimeManager.Instance.gameClockPause = false;
    }
}
