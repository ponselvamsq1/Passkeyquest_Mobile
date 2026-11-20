using System.Collections.Generic;
using UnityEngine;

// A different weather effect after every checkpoint (fog, snow, rain, leaves, sparkles...).
// Each checkpoint has a CheckpointEffect component: pick its weather in the Inspector.
// When the player reaches a checkpoint, the old weather fades out and that checkpoint's weather fades in.
// The weather follows the camera and is purely visual.


[DefaultExecutionOrder(10001)]
public class CheckpointWeather : MonoBehaviour
{
    [Tooltip("Weather from the start of the level until the first checkpoint.")]
    public WeatherKind startWeather = WeatherKind.None;

    Camera _cam;
    Transform _root;
    readonly Dictionary<WeatherKind, ParticleSystem[]> _fx = new Dictionary<WeatherKind, ParticleSystem[]>();
    CheckPointController[] _points;
    readonly HashSet<CheckPointController> _seen = new HashSet<CheckPointController>();
    WeatherKind _current = WeatherKind.None;

    // Weather the player is in now (the player uses it: slower in snow, slippery in rain).
    public static WeatherKind Current { get; private set; }
    void OnDestroy() => Current = WeatherKind.None;

    [Tooltip("Seconds a new weather takes to build up from nothing to full strength.")]
    public float rampTime = 6f;
    readonly Dictionary<ParticleSystem, float> _fullRate = new Dictionary<ParticleSystem, float>();
    float _rampStart = -100f;

    void Start()
    {
        Current = WeatherKind.None;
        _cam = Camera.main;
        _root = new GameObject("Weather").transform;
        _root.SetParent(transform, false);
        _points = FindObjectsByType<CheckPointController>(FindObjectsSortMode.None);

        // after a respawn the player stands at the last checkpoint reached: use its weather
        WeatherKind w = startWeather;
        var player = FindFirstObjectByType<PlayerStateManager>();
        float best = 4f;
        foreach (var p in _points)
        {
            if (p.ischeckpointreached) _seen.Add(p);
            if (player && PlayerPrefs.HasKey(GameEnums.ESpecialStates.Actived + p.gameObject.name))
            {
                float d = Vector2.Distance(player.transform.position, p.transform.position);
                if (d < best) { best = d; w = EffectOf(p); }
            }
        }
        SetWeather(w);
    }

    static WeatherKind EffectOf(CheckPointController p)
    {
        var e = p.GetComponent<CheckpointEffect>();
        return e ? e.weather : WeatherKind.None;
    }

    void Update()
    {
        // new weather starts light (a few drops / flakes) and slowly builds up to full strength
        if (_fx.TryGetValue(_current, out var live))
        {
            float k = Mathf.Clamp01((Time.unscaledTime - _rampStart) / Mathf.Max(0.1f, rampTime));
            k = k * k;   // gentle start
            foreach (var ps in live)
            {
                if (!ps || !_fullRate.TryGetValue(ps, out var full)) continue;
                var em = ps.emission;
                em.rateOverTime = full * k;
            }
        }
        foreach (var p in _points)
            if (p && p.ischeckpointreached && _seen.Add(p))
                SetWeather(EffectOf(p));
    }

    void LateUpdate()
    {
        if (!_cam) { _cam = Camera.main; if (!_cam) return; }
        var c = _cam.transform.position;
        _root.position = new Vector3(c.x, c.y, 0f);
    }

    public void SetWeather(WeatherKind w)
    {
        if (w == _current) return;
        if (_fx.TryGetValue(_current, out var old))
            foreach (var ps in old) if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);   // fades out naturally
        _current = w;
        Current = w;
        if (w == WeatherKind.None) return;
        if (!_fx.TryGetValue(w, out var list))
        {
            list = Build(w); _fx[w] = list;
            foreach (var ps in list)
                if (ps)
                {
                    _fullRate[ps] = ps.emission.rateOverTime.constant;
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // nothing on screen yet
                }
        }
        _rampStart = Time.unscaledTime;
        foreach (var ps in list)
        {
            if (!ps) continue;
            var main = ps.main; main.prewarm = false;   // start empty, then build up
            var em = ps.emission; em.rateOverTime = 0f;
            ps.Play(true);
        }
    }

    // Size of the area the camera sees, plus a margin so nothing pops in at the edges.
    Vector2 View()
    {
        float h = _cam && _cam.orthographic ? _cam.orthographicSize * 2f : 12f;
        float wv = h * (_cam ? _cam.aspect : 16f / 9f);
        return new Vector2(wv + 6f, h + 4f);
    }

    ParticleSystem[] Build(WeatherKind w)
    {
        var v = View();
        int order = AtmosphereFX.FrontOrder + 25;
        switch (w)
        {
            case WeatherKind.Fog:
                return new[] { Make("Fog", v, new Color(0.92f, 0.95f, 1f, 0.13f), 5f, 10f, 8f, 12f, v.x * v.y / 90f,
                                    new Vector2(0.4f, 0f), 0.3f, 0f, order, false, false) };
            case WeatherKind.Snow:
                return new[] { Make("Snow", v, new Color(1f, 1f, 1f, 0.95f), 0.18f, 0.42f, 6f, 9f, v.x * 2.5f,
                                    new Vector2(-0.4f, -1.4f), 0.6f, v.y * 0.5f, order, false, false) };
            case WeatherKind.Blizzard:
                return new[] { Make("Blizzard", v, new Color(1f, 1f, 1f, 0.9f), 0.14f, 0.32f, 2f, 3f, v.x * 8f,
                                    new Vector2(-6f, -3f), 1f, v.y * 0.5f, order, false, false),
                               Make("BlizzardHaze", v, new Color(0.9f, 0.95f, 1f, 0.15f), 5f, 9f, 6f, 9f, v.x * v.y / 60f,
                                    new Vector2(-1.5f, 0f), 0.3f, 0f, order - 1, false, false) };
            case WeatherKind.Rain:
                return new[] { Make("Rain", v, new Color(0.8f, 0.9f, 1f, 0.85f), 0.09f, 0.13f, 0.9f, 1.2f, v.x * 12f,
                                    new Vector2(-3f, -22f), 0f, v.y * 0.5f, order, true, false) };
            case WeatherKind.Leaves:
                return new[] { Make("Leaves", v, new Color(1f, 0.62f, 0.2f, 1f), 0.35f, 0.6f, 6f, 9f, v.x * 0.7f,
                                    new Vector2(-1.2f, -1.1f), 1.2f, v.y * 0.5f, order, false, true),
                               Make("LeavesGreen", v, new Color(0.55f, 0.8f, 0.25f, 1f), 0.3f, 0.5f, 6f, 9f, v.x * 0.4f,
                                    new Vector2(-1f, -1f), 1.2f, v.y * 0.5f, order, false, true) };
            case WeatherKind.Sparkles:
                return new[] { AtmosphereFX.Particles(_root, "Sparkles", v, new Color(1f, 0.95f, 0.6f, 1f), 0.15f, 0.35f, 2f, 4f,
                                    v.x * v.y / 12f, 0.4f, new Vector2(0f, 0.3f), order, true) };
        }
        return new ParticleSystem[0];
    }

    // Particles across the view (or along its top edge when topOffset > 0) moving with "drift".
    ParticleSystem Make(string name, Vector2 view, Color color, float minSize, float maxSize, float minLife, float maxLife,
                        float rate, Vector2 drift, float wander, float topOffset, int order, bool streak, bool spin)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_root, false);
        go.transform.localPosition = new Vector3(0f, topOffset, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;   // already falling across the screen when it starts
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.CeilToInt(rate * maxLife * 1.3f) + 8;
        main.useUnscaledTime = true;
        if (spin) main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var em = ps.emission;
        em.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        // falling weather starts along a strip above the view; fog fills the whole view
        shape.scale = topOffset > 0f ? new Vector3(view.x * 1.4f, 1f, 0.1f) : new Vector3(view.x, view.y, 0.1f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(drift.x * 0.7f, drift.x * 1.3f);
        vel.y = new ParticleSystem.MinMaxCurve(drift.y * 0.8f, drift.y * 1.2f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        if (wander > 0f)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = wander;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.3f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }
        if (spin)
        {
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
        }

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = AtmosphereFX.ParticleMat;
        r.sortingLayerName = AtmosphereFX.FrontLayer;
        r.sortingOrder = order;
        if (streak)
        {
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.06f;
            r.lengthScale = 1f;
        }
        return ps;
    }
}
