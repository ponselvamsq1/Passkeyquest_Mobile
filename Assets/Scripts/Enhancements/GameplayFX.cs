using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Player juice: dust on jump/land/dash, afterimage trail on dash, red flash + vibration on hit,
// freeze-frame + camera shake on enemy stomp. Reads the player's state; never changes it.
public class PlayerFX : MonoBehaviour
{
    PlayerStateManager _p;
    SpriteRenderer _sr;
    FieldInfo _stateField;
    string _lastState = "";
    float _nextGhost;
    static PlayerFX _inst;

    void Awake()
    {
        _inst = this;
        _p = GetComponent<PlayerStateManager>();
        _sr = GetComponent<SpriteRenderer>();
        _stateField = typeof(PlayerStateManager).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    void OnEnable()
    {
        if (!EventsManager.Instance) return;
        EventsManager.Instance.SubcribeToAnEvent(GameEnums.EEvents.PlayerOnTakeDamage, OnHit);
        EventsManager.Instance.SubcribeToAnEvent(GameEnums.EEvents.PlayerOnJumpPassive, OnStomp);
    }

    void OnDisable()
    {
        if (!EventsManager.Instance) return;
        EventsManager.Instance.UnSubcribeToAnEvent(GameEnums.EEvents.PlayerOnTakeDamage, OnHit);
        EventsManager.Instance.UnSubcribeToAnEvent(GameEnums.EEvents.PlayerOnJumpPassive, OnStomp);
    }

    void Update()
    {
        if (!EnhancementsBootstrap.IsActive || _stateField == null) return;
        var st = _stateField.GetValue(_p);
        string s = st != null ? st.GetType().Name : "";
        if (s != _lastState)
        {
            if (s == "JumpState" || s == "DoubleJumpState" || s == "WallJumpState") Dust(6, 0.9f);
            else if ((s == "IdleState" || s == "RunState") && (_lastState == "FallState" || _lastState == "JumpState" || _lastState == "DoubleJumpState")) Dust(8, 1.1f);
            else if (s == "DashState") Dust(5, 0.8f);
            _lastState = s;
        }
        if (s == "DashState" && Time.time >= _nextGhost) { _nextGhost = Time.time + 0.035f; Ghost(); }
    }

    void Dust(int count, float size)
    {
        if (!_sr) return;
        Vector3 feet = new Vector3(_sr.bounds.center.x, _sr.bounds.min.y + 0.05f, transform.position.z);
        for (int i = 0; i < count; i++) StartCoroutine(Puff(feet, size));
    }

    IEnumerator Puff(Vector3 at, float size)
    {
        var go = new GameObject("Dust");
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = JuiceSprites.Glow;
        r.color = new Color(0.95f, 0.9f, 0.8f, 0.8f);
        r.sortingLayerID = _sr.sortingLayerID;
        r.sortingOrder = _sr.sortingOrder + 1;
        go.transform.position = at;
        Vector3 vel = new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(0.2f, 1.0f), 0);
        float life = Random.Range(0.3f, 0.5f), t = 0, s0 = Random.Range(0.18f, 0.32f) * size;
        while (t < life)
        {
            t += Time.deltaTime;
            float p = t / life;
            go.transform.position += vel * Time.deltaTime;
            vel *= 1f - 4f * Time.deltaTime;
            go.transform.localScale = Vector3.one * s0 * (1f + p * 1.5f);
            r.color = new Color(0.95f, 0.9f, 0.8f, 0.8f * (1f - p));
            yield return null;
        }
        Destroy(go);
    }

    void Ghost()
    {
        if (!_sr || !_sr.sprite) return;
        var go = new GameObject("DashGhost");
        go.transform.SetPositionAndRotation(transform.position, transform.rotation);
        go.transform.localScale = transform.lossyScale;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = _sr.sprite; r.flipX = _sr.flipX;
        r.sortingLayerID = _sr.sortingLayerID; r.sortingOrder = _sr.sortingOrder - 1;
        StartCoroutine(FadeGhost(r));
    }

    IEnumerator FadeGhost(SpriteRenderer r)
    {
        float t = 0, life = 0.25f;
        while (t < life && r)
        {
            t += Time.deltaTime;
            r.color = new Color(0.4f, 0.9f, 1f, 0.55f * (1f - t / life));
            yield return null;
        }
        if (r) Destroy(r.gameObject);
    }

    void OnHit(object _)
    {
        if (!EnhancementsBootstrap.IsActive || !isActiveAndEnabled) return;
        StartCoroutine(RedFlash());
        Haptics.Buzz();
    }

    // Red copy of the current sprite laid over the player for a moment (player colour is untouched).
    IEnumerator RedFlash()
    {
        var go = new GameObject("HitFlash");
        go.transform.SetParent(transform, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sortingLayerID = _sr.sortingLayerID; r.sortingOrder = _sr.sortingOrder + 2;
        float t = 0, life = 0.3f;
        while (t < life)
        {
            t += Time.unscaledDeltaTime;
            r.sprite = _sr.sprite; r.flipX = _sr.flipX;
            r.color = new Color(1f, 0.1f, 0.1f, 0.75f * Mathf.PingPong(t * 10f, 1f) * (1f - t / life));
            yield return null;
        }
        Destroy(go);
    }

    void OnStomp(object _)
    {
        if (!EnhancementsBootstrap.IsActive || !isActiveAndEnabled) return;
        StartCoroutine(HitStop());
        if (EventsManager.Instance) EventsManager.Instance.NotifyObservers(GameEnums.EEvents.CameraOnShake, null);
        Dust(6, 1f);
        JuiceAudio.Play(JuiceAudio.Fx.Pop, 0.8f, 0.8f);
    }

    IEnumerator HitStop()
    {
        if (Time.timeScale != 1f) yield break; // don't fight pause menus
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(0.07f);
        if (Mathf.Approximately(Time.timeScale, 0.05f)) Time.timeScale = 1f;
    }
}

// On pickup: the item's sprite flies to the HP bar, "+1" floats up, a pop sound plays.
public class PickupFX : MonoBehaviour
{
    SpriteRenderer _sr;
    Sprite _last;
    bool _quitting;

    void Awake() => _sr = GetComponentInChildren<SpriteRenderer>();
    void LateUpdate() { if (_sr && _sr.sprite) _last = _sr.sprite; }
    void OnApplicationQuit() => _quitting = true;

    void OnDestroy()
    {
        if (_quitting || !EnhancementsBootstrap.IsActive || !gameObject.scene.isLoaded) return;
        var player = PlayerFXLookup.Player;
        if (!player || Vector2.Distance(player.position, transform.position) > 3f) return; // destroyed by something else
        var cam = Camera.main;
        if (!cam) return;
        Vector3 screen = cam.WorldToScreenPoint(transform.position);
        PickupFlyer.Spawn(_last, screen);
        PickupFlyer.FloatingText("+1", transform.position);
        JuiceAudio.Play(JuiceAudio.Fx.Pop, 0.6f, 1.3f);
    }
}

public static class PlayerFXLookup
{
    static Transform _p;
    public static Transform Player
    {
        get
        {
            if (!_p) { var m = Object.FindObjectOfType<PlayerStateManager>(); _p = m ? m.transform : null; }
            return _p;
        }
    }
}

public static class PickupFlyer
{
    public static void Spawn(Sprite sprite, Vector3 screenPos)
    {
        if (!sprite) return;
        var ui = JuiceUI.I;
        var img = JuiceUI.NewImage(ui.Root, "PickupFly", sprite, Color.white, new Vector2(80, 80));
        img.preserveAspect = true;
        ui.StartCoroutine(Fly(img, screenPos));
    }

    static IEnumerator Fly(Image img, Vector3 screenPos)
    {
        var rt = img.rectTransform;
        var root = JuiceUI.I.Root;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, null, out Vector2 from);
        Vector2 to = from + new Vector2(0, 200);
        var hearts = HPBarFX.Inst ? HPBarFX.Inst.TargetScreenPoint() : (Vector3?)null;
        if (hearts.HasValue) RectTransformUtility.ScreenPointToLocalPointInRectangle(root, hearts.Value, null, out to);
        Vector2 ctrl = (from + to) / 2f + new Vector2(0, 260);
        yield return JuiceTween.Run(0.6f, p =>
        {
            if (!rt) return;
            float e = JuiceTween.EaseInCubic(p);
            Vector2 a = Vector2.Lerp(from, ctrl, e), b = Vector2.Lerp(ctrl, to, e);
            rt.anchoredPosition = Vector2.Lerp(a, b, e);
            rt.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.5f, p);
        });
        if (HPBarFX.Inst) HPBarFX.Inst.Bump();
        if (rt) Object.Destroy(rt.gameObject);
    }

    public static void FloatingText(string text, Vector3 world)
    {
        var go = new GameObject("FloatText");
        go.transform.position = world + Vector3.up * 0.4f;
        var t = go.AddComponent<TextMeshPro>();
        t.text = text; t.fontSize = 5; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(1f, 0.9f, 0.3f);
        t.outlineWidth = 0.25f; t.outlineColor = new Color32(60, 30, 0, 255);
        t.sortingOrder = 200;
        JuiceUI.I.StartCoroutine(Rise(t));
    }

    static IEnumerator Rise(TextMeshPro t)
    {
        Vector3 start = t.transform.position;
        yield return JuiceTween.Run(0.9f, p =>
        {
            if (!t) return;
            t.transform.position = start + Vector3.up * JuiceTween.EaseOutCubic(p) * 1.2f;
            t.transform.localScale = Vector3.one * (p < 0.2f ? JuiceTween.EaseOutBack(p / 0.2f) : 1f);
            var c = t.color; c.a = 1f - Mathf.Clamp01((p - 0.6f) / 0.4f); t.color = c;
        });
        if (t) Object.Destroy(t.gameObject);
    }
}

// HP hearts: shake + red flash when you lose a heart, pop when you gain one.
public class HPBarFX : MonoBehaviour
{
    public static HPBarFX Inst;
    PlayerHealthManager _hp;
    Image[] _hearts;
    int _last = -1;

    void Awake()
    {
        Inst = this;
        _hp = GetComponent<PlayerHealthManager>();
        _hearts = UIPolish.GetField<Image[]>(_hp, "_uiHP");
    }

    public Vector3? TargetScreenPoint()
    {
        if (_hearts == null || _hearts.Length == 0 || !_hearts[0] || !_hearts[0].isActiveAndEnabled) return null;
        int i = Mathf.Clamp(_hp.CurrentHP - 1, 0, _hearts.Length - 1);
        return RectTransformUtility.WorldToScreenPoint(null, _hearts[i].transform.position);
    }

    public void Bump()
    {
        if (_hearts == null) return;
        foreach (var h in _hearts) if (h && h.isActiveAndEnabled) StartCoroutine(JuiceUI.Punch(h.transform, 0.2f, 0.3f));
    }

    void Update()
    {
        if (!EnhancementsBootstrap.IsActive || _hearts == null) return;
        int cur = _hp.CurrentHP;
        if (_last >= 0 && cur != _last)
        {
            if (cur < _last)
            {
                int lost = Mathf.Clamp(cur, 0, _hearts.Length - 1);
                if (_hearts[lost])
                {
                    StartCoroutine(JuiceUI.Shake((RectTransform)_hearts[lost].transform.parent, 12f, 0.4f));
                    StartCoroutine(JuiceUI.Punch(_hearts[lost].transform, 0.6f, 0.4f));
                    StartCoroutine(Tint(_hearts[lost]));
                }
            }
            else
            {
                int gained = Mathf.Clamp(cur - 1, 0, _hearts.Length - 1);
                if (_hearts[gained]) StartCoroutine(JuiceUI.Punch(_hearts[gained].transform, 0.5f, 0.4f));
            }
        }
        _last = cur;
    }

    IEnumerator Tint(Image img)
    {
        Color baseC = img.color;
        yield return JuiceTween.Run(0.45f, p => { if (img) img.color = Color.Lerp(new Color(1f, 0.25f, 0.25f, baseC.a), baseC, p); });
    }
}

// Checkpoint: sparkle burst and a "Checkpoint saved!" toast the first time it's touched.
public class CheckpointFX : MonoBehaviour
{
    bool _done;

    void OnTriggerEnter2D(Collider2D c)
    {
        if (_done || !EnhancementsBootstrap.IsActive || !c.CompareTag(GameConstants.PLAYER_TAG)) return;
        _done = true;
        JuiceUI.I.Toast("Checkpoint saved!", new Color(0.4f, 1f, 0.55f));
        StartCoroutine(Burst());
    }

    IEnumerator Burst()
    {
        var stars = new Transform[10];
        var vels = new Vector3[10];
        for (int i = 0; i < stars.Length; i++)
        {
            var go = new GameObject("CPStar");
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = JuiceSprites.Star; r.color = new Color(1f, 0.95f, 0.5f); r.sortingOrder = 150;
            go.transform.position = transform.position + Vector3.up * 0.8f;
            go.transform.localScale = Vector3.one * 0.6f;
            float a = i / (float)stars.Length * Mathf.PI * 2f;
            vels[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2.5f, 4f);
            stars[i] = go.transform;
        }
        yield return JuiceTween.Run(0.8f, p =>
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (!stars[i]) continue;
                stars[i].position += vels[i] * Time.unscaledDeltaTime * (1f - p);
                stars[i].localScale = Vector3.one * 0.6f * (1f - p);
                stars[i].Rotate(0, 0, 360f * Time.unscaledDeltaTime);
            }
        });
        foreach (var s in stars) if (s) Destroy(s.gameObject);
    }
}
