using System.Collections.Generic;
using UnityEngine;

// Level atmosphere: area-based fog, night sky, dark caves with a light around the player,
// fireflies, pollen, dust, embers and sun rays, plus glows on keys / exit gate and sparks on saws.
// Zones are plain GameObjects with an AtmosphereZone component: move them or change "size"
// in the Scene view (the area is drawn as a coloured box). Everything is purely visual:
// no colliders, no changes to the level, the player or any game script.

public enum AtmosphereKind { Fog, Night, DarkCave, Fireflies, Pollen, DustMotes, Embers, SunRays }

public class AtmosphereZone : MonoBehaviour
{
    public AtmosphereKind kind = AtmosphereKind.Fog;
    public Vector2 size = new Vector2(30f, 10f);
    [Tooltip("Night / DarkCave: how far outside the box the darkness fades in (world units).")]
    public float blend = 6f;
    [Range(0f, 1f)] public float intensity = 1f;
    public Color color = Color.white;
    [Tooltip("Shown as a short banner the first time the player enters (empty = none).")]
    public string areaTitle = "";

    public Rect WorldRect => new Rect((Vector2)transform.position - size * 0.5f, size);

    // 1 inside the box, fading to 0 at "blend" units outside.
    public float Weight(Vector2 p)
    {
        Rect r = WorldRect;
        float dx = Mathf.Max(r.xMin - p.x, 0f, p.x - r.xMax);
        float dy = Mathf.Max(r.yMin - p.y, 0f, p.y - r.yMax);
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        float w = blend <= 0.01f ? (d <= 0f ? 1f : 0f) : 1f - Mathf.SmoothStep(0f, 1f, d / blend);
        return w * intensity;
    }

    public bool Contains(Vector2 p) => WorldRect.Contains(p);

    readonly List<SpriteRenderer> _puffs = new List<SpriteRenderer>();
    readonly List<Vector3> _puffData = new List<Vector3>(); // x: speed, y: phase, z: base alpha

    void Start()
    {
        switch (kind)
        {
            case AtmosphereKind.Fog: BuildFog(); break;
            case AtmosphereKind.SunRays: BuildRays(); break;
            case AtmosphereKind.Night: BuildStars(); break;
            case AtmosphereKind.Fireflies:
                AtmosphereFX.Particles(transform, "Fireflies", size, color, 0.18f, 0.32f, 4f, 7f, Mathf.Max(8f, size.x * size.y / 40f), 0.7f, Vector2.zero, AtmosphereFX.FrontOrder + 20, true);
                break;
            case AtmosphereKind.Pollen:
                AtmosphereFX.Particles(transform, "Pollen", size, color, 0.06f, 0.14f, 6f, 10f, Mathf.Max(6f, size.x * size.y / 60f), 0.35f, new Vector2(0.5f, -0.15f), AtmosphereFX.FrontOrder - 50, false);
                break;
            case AtmosphereKind.DustMotes:
                AtmosphereFX.Particles(transform, "Dust", size, color, 0.05f, 0.12f, 6f, 11f, Mathf.Max(6f, size.x * size.y / 50f), 0.25f, new Vector2(0.1f, 0.05f), AtmosphereFX.FrontOrder - 50, false);
                break;
            case AtmosphereKind.Embers:
                AtmosphereFX.Particles(transform, "Embers", size, color, 0.06f, 0.16f, 3f, 6f, Mathf.Max(6f, size.x * size.y / 70f), 0.5f, new Vector2(0f, 0.9f), AtmosphereFX.FrontOrder + 20, true);
                break;
        }
    }

    // Soft noise clouds drifting sideways, half behind the level and half in front.
    void BuildFog()
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(size.x * size.y / 28f), 6, 40);
        var rnd = new System.Random(GetInstanceID());
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("FogPuff");
            go.transform.SetParent(transform, false);
            float fx = (float)rnd.NextDouble() - 0.5f, fy = (float)rnd.NextDouble() - 0.5f;
            go.transform.localPosition = new Vector3(fx * size.x, fy * size.y * 0.8f, 0f);
            float s = Mathf.Lerp(5f, 11f, (float)rnd.NextDouble());
            go.transform.localScale = new Vector3(s * 1.7f, s, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AtmosphereFX.FogSprite;
            // half sits over the background, half just behind the player/enemies (so they stay clear)
            bool front = i % 2 == 0;
            sr.sortingLayerName = front ? "Default" : AtmosphereFX.BackLayer;
            sr.sortingOrder = front ? -20 : 20;
            sr.flipX = rnd.NextDouble() > 0.5;
            float a = color.a * (front ? 0.7f : 0.95f);
            sr.color = new Color(color.r, color.g, color.b, a);
            _puffs.Add(sr);
            _puffData.Add(new Vector3(Mathf.Lerp(0.25f, 0.7f, (float)rnd.NextDouble()) * (rnd.NextDouble() > 0.3 ? 1 : -1),
                                      (float)rnd.NextDouble() * 6.28f, a));
        }
    }

    // Long soft light shafts slanting down from the sky, gently breathing.
    void BuildRays()
    {
        var rnd = new System.Random(GetInstanceID());
        int count = Mathf.Clamp(Mathf.RoundToInt(size.x / 9f), 3, 10);
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("SunRay");
            go.transform.SetParent(transform, false);
            float x = ((i + 0.5f) / count - 0.5f) * size.x + (float)(rnd.NextDouble() - 0.5) * 4f;
            go.transform.localPosition = new Vector3(x, size.y * 0.1f, 0f);
            go.transform.localRotation = Quaternion.Euler(0, 0, -28f + (float)(rnd.NextDouble() - 0.5) * 8f);
            go.transform.localScale = new Vector3(Mathf.Lerp(2.5f, 5f, (float)rnd.NextDouble()), size.y * 1.1f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AtmosphereFX.RaySprite;
            sr.sortingLayerName = AtmosphereFX.BackLayer;
            sr.sortingOrder = 30;
            sr.color = new Color(color.r, color.g, color.b, color.a * 0.5f);
            _puffs.Add(sr);
            _puffData.Add(new Vector3(0f, (float)rnd.NextDouble() * 6.28f, color.a * 0.5f));
        }
    }

    // Twinkling stars in the upper part of a night zone (drawn above the darkness).
    void BuildStars()
    {
        var area = new Vector2(size.x + blend * 2f, size.y * 0.45f);
        var ps = AtmosphereFX.Particles(transform, "Stars", area, new Color(1f, 1f, 0.9f, color.a), 0.06f, 0.16f, 3f, 6f,
            Mathf.Max(10f, area.x * area.y / 25f), 0f, Vector2.zero, AtmosphereFX.FrontOrder + 10, true);
        ps.transform.localPosition = new Vector3(0f, size.y * 0.5f - area.y * 0.5f, 0f);
        AtmosphereDirector.RegisterNightFX(this, ps.GetComponent<ParticleSystemRenderer>());
    }

    void Update()
    {
        if (_puffs.Count == 0) return;
        float t = Time.unscaledTime;
        float halfW = size.x * 0.5f + 4f;
        for (int i = 0; i < _puffs.Count; i++)
        {
            var sr = _puffs[i];
            var d = _puffData[i];
            if (kind == AtmosphereKind.Fog)
            {
                var lp = sr.transform.localPosition;
                lp.x += d.x * Time.unscaledDeltaTime;
                if (lp.x > halfW) lp.x = -halfW; else if (lp.x < -halfW) lp.x = halfW;
                lp.y += Mathf.Sin(t * 0.3f + d.y) * 0.05f * Time.unscaledDeltaTime;
                sr.transform.localPosition = lp;
                // fade near the zone's left/right ends so puffs never pop in
                float edge = Mathf.Clamp01((halfW - Mathf.Abs(lp.x)) / 4f);
                var c = sr.color; c.a = d.z * edge * (0.75f + 0.25f * Mathf.Sin(t * 0.4f + d.y)); sr.color = c;
            }
            else
            {
                var c = sr.color; c.a = d.z * (0.6f + 0.4f * Mathf.Sin(t * 0.5f + d.y)); sr.color = c;
            }
        }
    }

    void OnDrawGizmos()
    {
        Color c = kind switch
        {
            AtmosphereKind.Fog => new Color(0.8f, 0.85f, 1f),
            AtmosphereKind.Night => new Color(0.3f, 0.35f, 1f),
            AtmosphereKind.DarkCave => new Color(0.4f, 0.1f, 0.6f),
            AtmosphereKind.Fireflies => new Color(0.8f, 1f, 0.3f),
            AtmosphereKind.Embers => new Color(1f, 0.5f, 0.1f),
            AtmosphereKind.SunRays => new Color(1f, 0.9f, 0.4f),
            _ => new Color(1f, 1f, 1f)
        };
        Gizmos.color = new Color(c.r, c.g, c.b, 0.9f);
        Gizmos.DrawWireCube(transform.position, size);
        Gizmos.color = new Color(c.r, c.g, c.b, 0.06f);
        Gizmos.DrawCube(transform.position, size);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f), kind + (areaTitle != "" ? " - " + areaTitle : ""));
#endif
    }
}

// Shared procedural sprites (no image files) and particle helpers.
public static class AtmosphereFX
{
    public const string FrontLayer = "Fake_Map_Layer";
    public const string BackLayer = "BackGround_Layer";
    public const int FrontOrder = 400;
    public const float HoleInner = 0.06f;   // clear radius of HoleSprite, in sprite units (sprite is 1 unit wide)

    static Sprite _fog, _soft, _hole, _vig, _ray, _moon, _solid;

    // Plain white square, 1 unit wide.
    public static Sprite SolidSprite
    {
        get
        {
            if (_solid) return _solid;
            var t = Tex(4, 4); var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            t.SetPixels(px);
            return _solid = Make(t);
        }
    }
    static Material _particleMat;

    public static Color TitleColor(AtmosphereKind k) => k switch
    {
        AtmosphereKind.Night => new Color(0.55f, 0.65f, 1f),
        AtmosphereKind.DarkCave => new Color(0.9f, 0.55f, 1f),
        AtmosphereKind.Fog => new Color(0.75f, 0.9f, 1f),
        _ => new Color(1f, 0.85f, 0.35f)
    };

    static Texture2D Tex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        return t;
    }

    static Sprite Make(Texture2D t)
    {
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), t.width, 0, SpriteMeshType.FullRect);
    }

    // Cloudy noise blob for fog.
    public static Sprite FogSprite
    {
        get
        {
            if (_fog) return _fog;
            const int s = 128; var t = Tex(s, s); var px = new Color[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float u = x / (float)s, v = y / (float)s;
                float n = Mathf.PerlinNoise(u * 3f + 11f, v * 3f + 7f) * 0.6f + Mathf.PerlinNoise(u * 7f + 3f, v * 7f + 19f) * 0.4f;
                float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
                float fall = Mathf.Clamp01(1f - d); fall = fall * fall * (3f - 2f * fall);
                px[y * s + x] = new Color(1, 1, 1, Mathf.Clamp01(fall * (n * 1.4f - 0.15f)));
            }
            t.SetPixels(px);
            return _fog = Make(t);
        }
    }

    // Soft round glow.
    public static Sprite SoftSprite
    {
        get
        {
            if (_soft) return _soft;
            const int s = 64; var t = Tex(s, s); var px = new Color[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                float a = Mathf.Clamp01(1f - d); a *= a;
                px[y * s + x] = new Color(1, 1, 1, a);
            }
            t.SetPixels(px);
            return _soft = Make(t);
        }
    }

    // Opaque sheet with a soft transparent hole in the middle (darkness around the player).
    public static Sprite HoleSprite
    {
        get
        {
            if (_hole) return _hole;
            const int s = 256; var t = Tex(s, s); var px = new Color[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f)) / s;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HoleInner * 0.5f, HoleInner * 1.6f, d));
                px[y * s + x] = new Color(1, 1, 1, a);
            }
            t.SetPixels(px);
            return _hole = Make(t);
        }
    }

    // Dark edges, clear centre (screen vignette).
    public static Sprite VignetteSprite
    {
        get
        {
            if (_vig) return _vig;
            const int s = 128; var t = Tex(s, s); var px = new Color[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s * 2f - 1f, v = (y + 0.5f) / s * 2f - 1f;
                float d = Mathf.Sqrt(u * u * 0.8f + v * v);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 1.45f, d));
                px[y * s + x] = new Color(1, 1, 1, a);
            }
            t.SetPixels(px);
            return _vig = Make(t);
        }
    }

    // Vertical light shaft: bright at the top, fading down and at the sides.
    public static Sprite RaySprite
    {
        get
        {
            if (_ray) return _ray;
            const int w = 32, h = 128; var t = Tex(w, h); var px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f), v = (y + 0.5f) / h;
                float a = (1f - u * u) * Mathf.SmoothStep(0f, 1f, v) * Mathf.Clamp01((1f - v) * 8f);
                px[y * w + x] = new Color(1, 1, 1, a);
            }
            t.SetPixels(px);
            t.Apply();
            return _ray = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w, 0, SpriteMeshType.FullRect);
        }
    }

    // Crescent-lit moon disc with a few craters.
    public static Sprite MoonSprite
    {
        get
        {
            if (_moon) return _moon;
            const int s = 96; var t = Tex(s, s); var px = new Color[s * s];
            Vector2 c = new Vector2(s / 2f, s / 2f);
            Vector2[] craters = { new Vector2(0.38f, 0.6f), new Vector2(0.6f, 0.35f), new Vector2(0.55f, 0.68f) };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Vector2.Distance(p, c) / (s / 2f);
                float a = Mathf.Clamp01((1f - d) * s * 0.25f);
                float shade = 1f;
                foreach (var cr in craters)
                    if (Vector2.Distance(p / s, cr) < 0.08f) shade = 0.86f;
                shade *= Mathf.Lerp(1f, 0.82f, Mathf.Clamp01((p.x - p.y) / s + 0.3f));
                px[y * s + x] = new Color(shade, shade, shade * 0.96f, a);
            }
            t.SetPixels(px);
            return _moon = Make(t);
        }
    }

    public static Material ParticleMat
    {
        get
        {
            if (_particleMat) return _particleMat;
            _particleMat = new Material(Shader.Find("Sprites/Default")) { name = "AtmosphereParticles" };
            _particleMat.mainTexture = SoftSprite.texture;
            return _particleMat;
        }
    }

    // Generic soft-dot particle field filling a box.
    public static ParticleSystem Particles(Transform parent, string name, Vector2 box, Color color,
        float minSize, float maxSize, float minLife, float maxLife, float rate, float wander, Vector2 drift, int order, bool twinkle)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.CeilToInt(rate * maxLife * 1.2f) + 4;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.useUnscaledTime = true;   // ambience keeps moving while the level is paused

        var em = ps.emission;
        em.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(box.x, box.y, 0.1f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = drift != Vector2.zero;
        if (vel.enabled)
        {
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(drift.x * 0.6f, drift.x * 1.4f);
            vel.y = new ParticleSystem.MinMaxCurve(drift.y * 0.6f, drift.y * 1.4f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        if (wander > 0f)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = wander;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.2f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        if (twinkle)
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.25f, 0.35f),
                              new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0.4f, 0.8f), new GradientAlphaKey(0f, 1f) });
        else
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = ParticleMat;
        r.sortingLayerName = FrontLayer;
        r.sortingOrder = order;

        ps.Play();
        return ps;
    }

    // Small orange sparks thrown off a spinning saw blade.
    public static void Sparks(Transform saw)
    {
        var go = new GameObject("Sparks");
        go.transform.SetParent(saw, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.5f, 0.15f));
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        var em = ps.emission;
        em.rateOverTime = 14f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        shape.radiusThickness = 0f;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = ParticleMat;
        r.sortingLayerName = FrontLayer;
        r.sortingOrder = FrontOrder + 20;
        ps.Play();
    }
}
