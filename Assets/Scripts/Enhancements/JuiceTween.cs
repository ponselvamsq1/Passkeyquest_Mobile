using System;
using System.Collections;
using UnityEngine;

// Lightweight coroutine tweens. All use unscaled time so they work while the game is paused (Time.timeScale = 0).
public static class JuiceTween
{
    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
    }

    public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3);
    public static float EaseInCubic(float t) => t * t * t;

    public static IEnumerator Run(float duration, Action<float> step)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            step(Mathf.Clamp01(t / duration));
            yield return null;
        }
        step(1f);
    }

    public static IEnumerator Scale(Transform tr, Vector3 from, Vector3 to, float duration, Func<float, float> ease)
    {
        return Run(duration, p => { if (tr) tr.localScale = Vector3.LerpUnclamped(from, to, ease(p)); });
    }
}
