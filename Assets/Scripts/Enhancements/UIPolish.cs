using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One-shot UI upgrades applied at scene load (Start Menu + Level 1 only).
public static class UIPolish
{
    const float MinTouch = 110f;

    public static void ApplyAll(int buildIndex)
    {
        foreach (var b in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (!b.gameObject.scene.IsValid()) continue;
            var img = b.GetComponent<Image>();
            if (img && img.sprite && img.sprite.name == "close button") StyleCloseButton(b);
            EnsureTouchArea(b);
        }
        // (typewriter tick sound removed on request)

        var ui = UIManager.Instance;
        if (ui)
        {
            var win = GetField<GameObject>(ui, "_winPanel");
            var lose = GetField<GameObject>(ui, "_loosePanel");
            if (win && !win.GetComponent<WinLoseFX>()) win.AddComponent<WinLoseFX>().IsWin = true;
            if (lose && !lose.GetComponent<WinLoseFX>()) lose.AddComponent<WinLoseFX>().IsWin = false;
        }

        if (buildIndex == 0 && ui && ui.StartMenuCanvas && !ui.StartMenuCanvas.GetComponent<MenuIntro>())
            ui.StartMenuCanvas.AddComponent<MenuIntro>();

        if (buildIndex == 1)
        {
            foreach (var q in Resources.FindObjectsOfTypeAll<Quiz>())
            {
                if (!q.gameObject.scene.IsValid() || q.GetComponent<QuizOptionStyler>()) continue;
                q.gameObject.AddComponent<QuizOptionStyler>();
                Tag(q.correctpane, QuizPanelFX.Kind.Correct);
                Tag(q.wrongpanel, QuizPanelFX.Kind.Wrong);
                Tag(q.finalpanel, QuizPanelFX.Kind.Reveal);
                Tag(q.quizfinsihpanel, QuizPanelFX.Kind.Finish);
            }
        }
    }

    static void Tag(GameObject go, QuizPanelFX.Kind kind)
    {
        if (go && !go.GetComponent<QuizPanelFX>()) go.AddComponent<QuizPanelFX>().Type = kind;
    }

    public static T GetField<T>(object o, string name) where T : class
    {
        var f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return f?.GetValue(o) as T;
    }

    // Solid red circle behind the white X so it's easy to spot.
    static void StyleCloseButton(Button b)
    {
        var rt = (RectTransform)b.transform;
        if (rt.Find("CloseBadge")) return;
        // draw order: white rim -> red badge -> X
        var rim = JuiceUI.NewImage(rt, "CloseRim", JuiceSprites.Circle, Color.white, rt.rect.size * 1.15f);
        var badge = JuiceUI.NewImage(rt, "CloseBadge", JuiceSprites.Circle, new Color(0.86f, 0.16f, 0.2f), rt.rect.size * 1.0f);
        // Children draw over the parent image, so redraw the X on top.
        var x = JuiceUI.NewImage(rt, "X", b.GetComponent<Image>().sprite, Color.white, rt.rect.size * 0.8f);
        x.preserveAspect = true;
        b.GetComponent<Image>().color = new Color(1, 1, 1, 0); // keep the image as the hit area, hide its pixels
        rim.rectTransform.anchoredPosition = Vector2.zero;
    }

    // Invisible hit area so small buttons are at least ~110px for fingers.
    static void EnsureTouchArea(Button b)
    {
        var rt = (RectTransform)b.transform;
        Vector2 size = rt.rect.size;
        if ((size.x >= MinTouch && size.y >= MinTouch) || rt.Find("TouchArea")) return;
        var img = JuiceUI.NewImage(rt, "TouchArea", JuiceSprites.White, new Color(1, 1, 1, 0), new Vector2(Mathf.Max(size.x, MinTouch), Mathf.Max(size.y, MinTouch)));
        img.raycastTarget = true;
        img.transform.SetAsFirstSibling();
    }
}

// "75%" readout and a mute toggle next to a volume slider.
public class SliderExtras : MonoBehaviour
{
    Slider _s;
    TextMeshProUGUI _pct, _muteTxt;
    Image _muteBg;
    float _beforeMute = 1f;

    void Awake()
    {
        _s = GetComponent<Slider>();
        var rt = (RectTransform)transform;
        _pct = JuiceUI.NewText(rt, "", 30, Color.white);
        var prt = _pct.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(1f, 0.5f);
        prt.sizeDelta = new Vector2(110, 44);
        prt.anchoredPosition = new Vector2(75, 0);

        _muteBg = JuiceUI.NewImage(rt, "MuteBtn", JuiceSprites.Pill, new Color(0.15f, 0.65f, 0.3f), new Vector2(120, 54));
        _muteBg.type = Image.Type.Sliced;
        _muteBg.raycastTarget = true;
        var mrt = _muteBg.rectTransform;
        mrt.anchorMin = mrt.anchorMax = new Vector2(1f, 0.5f);
        mrt.anchoredPosition = new Vector2(200, 0);
        var btn = _muteBg.gameObject.AddComponent<Button>();
        btn.onClick.AddListener(ToggleMute);
        _muteBg.gameObject.AddComponent<ButtonJuice>();
        _muteTxt = JuiceUI.NewText(mrt, "ON", 28, Color.white);
        _muteTxt.rectTransform.sizeDelta = new Vector2(120, 54);

        _s.onValueChanged.AddListener(_ => Refresh());
        Refresh();
    }

    void ToggleMute()
    {
        if (_s.value > _s.minValue + 0.001f) { _beforeMute = _s.value; _s.value = _s.minValue; }
        else _s.value = _beforeMute > _s.minValue + 0.001f ? _beforeMute : _s.maxValue;
        JuiceAudio.Play(JuiceAudio.Fx.Pop, 0.8f);
    }

    void Refresh()
    {
        float n = Mathf.InverseLerp(_s.minValue, _s.maxValue, _s.value);
        _pct.text = Mathf.RoundToInt(n * 100f) + "%";
        bool muted = n <= 0.001f;
        _muteTxt.text = muted ? "OFF" : "ON";
        _muteBg.color = muted ? new Color(0.7f, 0.18f, 0.2f) : new Color(0.15f, 0.65f, 0.3f);
    }
}

// Soft tick as each character of the intro typewriter appears.
public class TypewriterTick : MonoBehaviour
{
    TMP_Text _t; int _len;
    void Awake() { _t = GetComponent<TMP_Text>(); _len = _t.text.Length; }
    void LateUpdate()
    {
        int l = _t.text.Length;
        if (l > _len && !char.IsWhiteSpace(_t.text[l - 1]))
            JuiceAudio.Play(JuiceAudio.Fx.Tick, 0.5f, Random.Range(0.9f, 1.15f));
        _len = l;
    }
}

// Start Menu: buttons slide up one after another; clouds drift.
public class MenuIntro : MonoBehaviour
{
    void OnEnable() { if (EnhancementsBootstrap.IsActive) StartCoroutine(Play()); }

    IEnumerator Play()
    {
        var container = transform.Find("BtnContainer");
        if (container)
        {
            int i = 0;
            foreach (RectTransform b in container)
            {
                if (!b.gameObject.activeSelf) continue;
                StartCoroutine(SlideIn(b, i++ * 0.12f));
            }
        }
        foreach (Transform c in transform)
            if (c.name == "Cloud") StartCoroutine(Drift((RectTransform)c));
        yield break;
    }

    IEnumerator SlideIn(RectTransform b, float delay)
    {
        var cg = b.GetComponent<CanvasGroup>();
        if (!cg) cg = b.gameObject.AddComponent<CanvasGroup>();
        Vector2 end = b.anchoredPosition, start = end + new Vector2(0, -160);
        cg.alpha = 0; b.anchoredPosition = start;
        yield return new WaitForSecondsRealtime(0.25f + delay);
        JuiceAudio.Play(JuiceAudio.Fx.Whoosh, 0.35f, 1.3f);
        yield return JuiceTween.Run(0.45f, p =>
        {
            if (!b) return;
            b.anchoredPosition = Vector2.LerpUnclamped(start, end, JuiceTween.EaseOutBack(p));
            cg.alpha = p;
        });
    }

    IEnumerator Drift(RectTransform c)
    {
        Vector2 basePos = c.anchoredPosition;
        float phase = Random.value * 10f;
        while (c && c.gameObject.activeInHierarchy && EnhancementsBootstrap.IsActive)
        {
            float t = Time.unscaledTime + phase;
            c.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * 0.15f) * 45f, Mathf.Sin(t * 0.4f) * 6f);
            yield return null;
        }
        if (c) c.anchoredPosition = basePos;
    }
}

// Win: confetti + three stars pop in one by one. Lose: screen fades to grey.
public class WinLoseFX : MonoBehaviour
{
    public bool IsWin;
    GameObject _layer;

    void OnEnable()
    {
        if (!EnhancementsBootstrap.IsActive) return;
        StartCoroutine(IsWin ? Win() : Lose());
    }

    void OnDisable() { if (_layer) Destroy(_layer); }

    IEnumerator Win()
    {
        JuiceUI.I.ConfettiBurst(90);
        JuiceAudio.Play(JuiceAudio.Fx.Chime);
        _layer = new GameObject("WinStars", typeof(RectTransform));
        var rt = (RectTransform)_layer.transform;
        rt.SetParent(JuiceUI.I.Root, false);
        rt.anchoredPosition = new Vector2(0, 250);
        for (int i = 0; i < 3; i++)
        {
            var s = JuiceUI.NewImage(rt, "Star", JuiceSprites.Star, new Color(1f, 0.85f, 0.2f), Vector2.one * (i == 1 ? 170 : 130));
            s.rectTransform.anchoredPosition = new Vector2((i - 1) * 170, i == 1 ? 30 : 0);
            s.transform.localScale = Vector3.zero;
        }
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSecondsRealtime(0.3f);
            var s = rt.GetChild(i);
            JuiceAudio.Play(JuiceAudio.Fx.StarDing, 0.9f, 1f + i * 0.15f);
            yield return JuiceTween.Run(0.35f, p => { if (s) s.localScale = Vector3.one * JuiceTween.EaseOutBack(p); });
        }
    }

    IEnumerator Lose()
    {
        _layer = JuiceUI.NewImage(transform, "GreyFade", JuiceSprites.White, new Color(0.15f, 0.15f, 0.18f, 0f), Vector2.zero).gameObject;
        var rt = (RectTransform)_layer.transform;
        rt.SetAsFirstSibling();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = _layer.GetComponent<Image>();
        JuiceAudio.Play(JuiceAudio.Fx.Buzz, 0.6f, 0.7f);
        yield return JuiceTween.Run(1.2f, p => { if (img) img.color = new Color(0.15f, 0.15f, 0.18f, 0.7f * p); });
    }
}
