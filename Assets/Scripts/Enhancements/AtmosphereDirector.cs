using System.Collections.Generic;
using UnityEngine;

// Lives on the "==Atmosphere==" object: darkness + light around the player, moon,
// screen vignette, area banners, key / gate glows and saw sparks.
// Runs after CinemachineBrain so camera-anchored sprites (moon, tint) use this frame's camera position.
[DefaultExecutionOrder(10000)]
public class AtmosphereDirector : MonoBehaviour
{
    [Header("Darkness")]
    public Color nightColor = new Color(0.02f, 0.04f, 0.14f);
    public Color caveColor = new Color(0.03f, 0.02f, 0.06f);
    [Tooltip("Radius of the clear area around the player in caves (world units).")]
    public float lightRadius = 2.6f;
    [Range(0f, 1f)] public float vignette = 0.35f;
    [Header("Extras")]
    public bool keyGlow = true;
    public bool sawSparks = true;
    public bool exitGlow = true;

    AtmosphereZone[] _zones;
    Camera _cam;
    Transform _player;
    SpriteRenderer _tint, _dark, _warm, _vig, _moon, _moonGlow;
    TMPro.TMP_Text _keysText;
    string _keysLast;
    Color _keysColor;
    Vector3 _keysScale;
    float _keysFlash;
    float _night, _cave, _lastBanner = -10f;
    AtmosphereZone _pendingTitle;
    readonly HashSet<AtmosphereZone> _announced = new HashSet<AtmosphereZone>();
    readonly List<(Transform t, SpriteRenderer sr, float phase, float baseA)> _glows = new List<(Transform, SpriteRenderer, float, float)>();
    static readonly List<(AtmosphereZone z, ParticleSystemRenderer r)> _nightFx = new List<(AtmosphereZone, ParticleSystemRenderer)>();

    public static void RegisterNightFX(AtmosphereZone z, ParticleSystemRenderer r) => _nightFx.Add((z, r));

    void Awake() => _nightFx.Clear();

    void Start()
    {
        _zones = GetComponentsInChildren<AtmosphereZone>();
        _lastBanner = Time.unscaledTime + 2f;   // first banner after the "LEVEL 1" title card
        _cam = Camera.main;
        var p = FindFirstObjectByType<PlayerStateManager>();
        if (p) _player = p.transform;

        _tint = MakeSR("NightTint", AtmosphereFX.SolidSprite, AtmosphereFX.FrontOrder - 1);
        _dark = MakeSR("Darkness", AtmosphereFX.HoleSprite, AtmosphereFX.FrontOrder);
        _warm = MakeSR("PlayerLight", AtmosphereFX.SoftSprite, AtmosphereFX.FrontOrder + 1);
        _vig = MakeSR("Vignette", AtmosphereFX.VignetteSprite, AtmosphereFX.FrontOrder + 30);
        _moonGlow = MakeSR("MoonGlow", AtmosphereFX.SoftSprite, AtmosphereFX.FrontOrder + 5);
        _moon = MakeSR("Moon", AtmosphereFX.MoonSprite, AtmosphereFX.FrontOrder + 6);

        if (keyGlow)
            foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (sr.gameObject.name.StartsWith("Key_") && sr.transform.parent == null)
                    AddGlow(sr.transform, KeyColor(sr.gameObject.name), 2.2f, sr.sortingLayerName, sr.sortingOrder - 1, true);
        if (exitGlow)
        {
            var gate = GameObject.Find("GreenGate");
            if (gate)
            {
                AddGlow(gate.transform, new Color(0.4f, 1f, 0.5f, 0.55f), 6f, "Default", -2, false);
                AtmosphereFX.Particles(gate.transform, "GateMotes", new Vector2(2.5f, 2.5f), new Color(0.6f, 1f, 0.6f, 1f), 0.08f, 0.16f, 1.5f, 2.5f, 8f, 0.2f, new Vector2(0f, 0.8f), AtmosphereFX.FrontOrder + 20, true);
            }
        }
        foreach (var t in FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
            if (t.text.ToUpper().Contains("KEYS")) { _keysText = t; _keysLast = t.text; _keysColor = t.color; _keysScale = t.transform.localScale; break; }
        if (sawSparks)
            foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (sr.gameObject.name == "MainObject" && sr.transform.parent && sr.transform.parent.name.StartsWith("Saw"))
                    AtmosphereFX.Sparks(sr.transform);
    }

    static Color KeyColor(string n)
    {
        if (n.Contains("Blue")) return new Color(0.4f, 0.7f, 1f, 0.75f);
        if (n.Contains("Green")) return new Color(0.4f, 1f, 0.5f, 0.75f);
        if (n.Contains("Red")) return new Color(1f, 0.4f, 0.4f, 0.75f);
        return new Color(1f, 0.85f, 0.3f, 0.75f);
    }

    void AddGlow(Transform target, Color c, float size, string layer, int order, bool sparkle)
    {
        var go = new GameObject("Glow");
        go.transform.SetParent(target, false);
        var ls = target.lossyScale;
        go.transform.localScale = new Vector3(size / Mathf.Max(0.01f, ls.x), size / Mathf.Max(0.01f, ls.y), 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AtmosphereFX.SoftSprite;
        sr.color = c;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        _glows.Add((go.transform, sr, Random.value * 6f, c.a));
        if (sparkle)
            AtmosphereFX.Particles(target, "KeySparkle", new Vector2(1.2f, 1.2f), new Color(1f, 1f, 0.85f, 1f), 0.08f, 0.18f, 0.6f, 1.2f, 5f, 0f, Vector2.zero, AtmosphereFX.FrontOrder + 20, true);
    }

    SpriteRenderer MakeSR(string name, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = AtmosphereFX.FrontLayer;
        sr.sortingOrder = order;
        sr.color = Color.clear;
        return sr;
    }

    void LateUpdate()
    {
        if (!_cam) { _cam = Camera.main; if (!_cam) return; }
        Vector3 camPos = _cam.transform.position;
        Vector2 focus = _player ? (Vector2)_player.position : (Vector2)camPos;

        // how much night / cave darkness at the player's position
        float night = 0f, cave = 0f;
        foreach (var z in _zones)
        {
            if (!z || !z.isActiveAndEnabled) continue;
            if (z.kind == AtmosphereKind.Night) night = Mathf.Max(night, z.Weight(focus));
            else if (z.kind == AtmosphereKind.DarkCave) cave = Mathf.Max(cave, z.Weight(focus));
            if (z.areaTitle != "" && !_announced.Contains(z) && z.Contains(focus))
            {
                _announced.Add(z);
                _pendingTitle = z;   // only the latest area is announced
            }
        }
        // one banner at a time (each one is on screen for about 2.5 s)
        if (_pendingTitle && Time.unscaledTime - _lastBanner > 2.8f && PopupDecorator.OpenCount == 0 && Time.timeScale > 0f)
        {
            // area-name banner removed
            // if (_pendingTitle.Contains(focus)) JuiceUI.I.Toast(_pendingTitle.areaTitle, AtmosphereFX.TitleColor(_pendingTitle.kind));
            _lastBanner = Time.unscaledTime;
            _pendingTitle = null;
        }
        float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 3f);   // real time: the level can start paused
        _night = Mathf.Lerp(_night, night, k);
        _cave = Mathf.Lerp(_cave, cave, k);

        float viewH = _cam.orthographicSize * 2f, viewW = viewH * _cam.aspect;

        // night: an even blue tint over the whole view (the sky turns to night)
        _tint.transform.position = new Vector3(camPos.x, camPos.y, 0f);
        _tint.transform.localScale = new Vector3(viewW * 1.1f, viewH * 1.1f, 1f);
        _tint.color = new Color(nightColor.r, nightColor.g, nightColor.b, _night * 0.5f);
        _tint.enabled = _night > 0.005f;

        // darkness: a big sheet with a soft hole around the player
        float darkA = Mathf.Max(_night * 0.55f, _cave * 0.88f);
        float caveShare = _cave * 0.88f >= _night * 0.55f ? 1f : 0f;
        Color dc = Color.Lerp(nightColor, caveColor, caveShare);
        _dark.color = new Color(dc.r, dc.g, dc.b, darkA);
        _dark.enabled = darkA > 0.005f;
        float radius = Mathf.Lerp(lightRadius * 1.6f, lightRadius, caveShare) * (1f + 0.025f * Mathf.Sin(Time.unscaledTime * 2.3f));
        _dark.transform.position = new Vector3(focus.x, focus.y, 0f);
        _dark.transform.localScale = Vector3.one * (radius / AtmosphereFX.HoleInner);
        // make sure the sheet always covers the screen
        float need = (Vector2.Distance(focus, camPos) + Mathf.Sqrt(viewW * viewW + viewH * viewH) * 0.5f) * 2.1f;
        if (_dark.transform.localScale.x < need) _dark.transform.localScale = Vector3.one * need;

        // warm light around the player
        _warm.transform.position = new Vector3(focus.x, focus.y, 0f);
        _warm.transform.localScale = Vector3.one * radius * 2.4f;
        _warm.color = new Color(1f, 0.75f, 0.4f, 0.16f * darkA);
        _warm.enabled = darkA > 0.005f;

        // moon in the upper right of the screen, with a little parallax
        float m = _night;
        Vector3 moonPos = new Vector3(camPos.x + viewW * 0.32f - camPos.x * 0.02f, camPos.y + viewH * 0.3f, 0f);
        _moon.transform.position = moonPos;
        _moon.transform.localScale = Vector3.one * 1.6f;
        _moon.color = new Color(1f, 0.98f, 0.85f, m);
        _moonGlow.transform.position = moonPos;
        _moonGlow.transform.localScale = Vector3.one * 7f;
        _moonGlow.color = new Color(0.7f, 0.8f, 1f, 0.35f * m);
        _moon.enabled = _moonGlow.enabled = m > 0.01f;
        foreach (var (z, r) in _nightFx) if (r) r.enabled = _night > 0.05f;

        // soft screen vignette, a bit stronger in the dark
        _vig.transform.position = new Vector3(camPos.x, camPos.y, 0f);
        _vig.transform.localScale = new Vector3(viewW * 1.02f, viewH * 1.02f, 1f);
        _vig.color = new Color(0f, 0f, 0.02f, Mathf.Clamp01(vignette + darkA * 0.25f));

        // HUD: the "KEYS : x/5" counter pops and flashes gold when a key is collected
        if (_keysText)
        {
            if (_keysText.text != _keysLast)
            {
                _keysLast = _keysText.text;
                _keysFlash = 1f;
                StopAllCoroutines();
                _keysText.transform.localScale = _keysScale;
                StartCoroutine(JuiceUI.Punch(_keysText.transform, 0.45f, 0.4f));
            }
            if (_keysFlash > 0f)
            {
                _keysFlash = Mathf.Max(0f, _keysFlash - Time.unscaledDeltaTime * 1.2f);
                _keysText.color = Color.Lerp(_keysColor, new Color(1f, 0.8f, 0.15f), _keysFlash);
            }
        }

        // pulsing glows on keys / gate
        float t = Time.unscaledTime;
        for (int i = _glows.Count - 1; i >= 0; i--)
        {
            var g = _glows[i];
            if (!g.t) { _glows.RemoveAt(i); continue; }
            float s = 0.5f + 0.5f * Mathf.Sin(t * 2.4f + g.phase);
            var c = g.sr.color; c.a = g.baseA * (0.55f + 0.45f * s); g.sr.color = c;
        }
    }
}

