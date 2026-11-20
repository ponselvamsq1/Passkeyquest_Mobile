using System.Reflection;
using UnityEngine;

// Small synthesized sound effects (no new audio files). Volume follows the game's SFX slider.
public static class JuiceAudio
{
    public enum Fx { Tick, Chime, Buzz, Pop, Whoosh, Hover, StarDing }

    static AudioSource _src;
    static AudioClip[] _clips;
    const int Rate = 44100;

    static AudioSource Source
    {
        get
        {
            if (_src) return _src;
            var go = new GameObject("JuiceAudio");
            Object.DontDestroyOnLoad(go);
            _src = go.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.ignoreListenerPause = true;
            return _src;
        }
    }

    static float SfxVolume
    {
        get
        {
            var sm = SoundsManager.Instance;
            if (!sm) return 1f;
            var f = typeof(SoundsManager).GetField("_sfxSource", BindingFlags.Instance | BindingFlags.NonPublic);
            var s = f?.GetValue(sm) as AudioSource;
            return s ? s.volume : 1f;
        }
    }

    public static void Play(Fx fx, float volume = 1f, float pitch = 1f)
    {
        if (!EnhancementsBootstrap.IsActive) return;
        if (_clips == null) Build();
        Source.pitch = pitch;
        Source.PlayOneShot(_clips[(int)fx], volume * SfxVolume);
    }

    static void Build()
    {
        _clips = new AudioClip[7];
        _clips[(int)Fx.Tick] = Synth("tick", 0.025f, (t, p) => Square(t, 1800f) * Env(p, 0.001f, 1f) * 0.25f);
        _clips[(int)Fx.Hover] = Synth("hover", 0.05f, (t, p) => Sine(t, 1200f + 600f * p) * Env(p, 0.05f, 1f) * 0.25f);
        _clips[(int)Fx.Pop] = Synth("pop", 0.09f, (t, p) => Sine(t, Mathf.Lerp(900f, 300f, p)) * Env(p, 0.01f, 1f) * 0.6f);
        _clips[(int)Fx.Whoosh] = Synth("whoosh", 0.3f, (t, p) => Noise() * Mathf.Sin(p * Mathf.PI) * 0.25f);
        _clips[(int)Fx.Buzz] = Synth("buzz", 0.35f, (t, p) => (Square(t, 110f) * 0.6f + Square(t, 116f) * 0.4f) * Env(p, 0.02f, 1f) * 0.35f);
        _clips[(int)Fx.StarDing] = Synth("ding", 0.35f, (t, p) => (Sine(t, 1320f) + 0.4f * Sine(t, 2640f)) * Env(p, 0.005f, 1f) * 0.4f);
        // happy rising arpeggio C-E-G-C
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        _clips[(int)Fx.Chime] = Synth("chime", 0.6f, (t, p) =>
        {
            float v = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float start = i * 0.09f;
                if (t < start) continue;
                float lt = t - start;
                v += (Sine(lt, notes[i]) + 0.3f * Sine(lt, notes[i] * 2f)) * Mathf.Exp(-lt * 7f);
            }
            return v * 0.3f;
        });
    }

    static float Sine(float t, float f) => Mathf.Sin(2f * Mathf.PI * f * t);
    static float Square(float t, float f) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t));
    static float Noise() => Random.value * 2f - 1f;
    static float Env(float p, float attack, float decayPow) => (p < attack ? p / attack : Mathf.Pow(1f - (p - attack) / (1f - attack), 2f * decayPow));

    static AudioClip Synth(string name, float seconds, System.Func<float, float, float> f)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate, i / (float)n), -1f, 1f);
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

public static class Haptics
{
    public static void Buzz()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (EnhancementsBootstrap.IsActive) Handheld.Vibrate();
#endif
    }
}
