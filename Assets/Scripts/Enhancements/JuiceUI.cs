using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared screen-space helpers: overlay canvas, confetti, toast, screen flash, panel shake.
public class JuiceUI : MonoBehaviour
{
    static JuiceUI _inst;
    Canvas _canvas;

    public static JuiceUI I
    {
        get
        {
            if (_inst) return _inst;
            var go = new GameObject("JuiceOverlay", typeof(Canvas), typeof(CanvasScaler));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 60;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            _inst = go.AddComponent<JuiceUI>();
            _inst._canvas = c;
            return _inst;
        }
    }

    public RectTransform Root => (RectTransform)transform;

    public static Image NewImage(Transform parent, string name, Sprite sprite, Color color, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = sprite; img.color = color; img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI NewText(Transform parent, string text, float size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        t.enableWordWrapping = false;
        SetOutline(t, 0.2f, new Color32(0, 0, 0, 200));
        ((RectTransform)go.transform).sizeDelta = new Vector2(900, size * 1.4f);
        return t;
    }

    // TMP can only change its outline once it has initialised (i.e. been shown once); text made
    // under a hidden panel gets the outline the first time it appears instead.
    public static void SetOutline(TMP_Text t, float width, Color32 color)
    {
        if (t.gameObject.activeInHierarchy) { t.outlineWidth = width; t.outlineColor = color; return; }
        var d = t.GetComponent<DeferredOutline>();
        if (!d) d = t.gameObject.AddComponent<DeferredOutline>();
        d.Width = width; d.Color = color;
    }

    static readonly Color[] Confetti = {
        new Color(1f, 0.33f, 0.4f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.85f, 1f),
        new Color(0.45f, 1f, 0.45f), new Color(0.85f, 0.5f, 1f), Color.white };

    public void ConfettiBurst(int count = 70)
    {
        for (int i = 0; i < count; i++) StartCoroutine(ConfettiPiece());
    }

    IEnumerator ConfettiPiece()
    {
        var img = NewImage(Root, "Confetti", JuiceSprites.White, Confetti[Random.Range(0, Confetti.Length)],
            new Vector2(Random.Range(12f, 22f), Random.Range(20f, 34f)));
        var rt = img.rectTransform;
        Vector2 pos = new Vector2(Random.Range(-200f, 200f), -200f);
        Vector2 vel = new Vector2(Random.Range(-900f, 900f), Random.Range(900f, 1700f));
        float spin = Random.Range(-720f, 720f), life = Random.Range(1.6f, 2.4f), t = 0f;
        while (t < life)
        {
            float dt = Time.unscaledDeltaTime; t += dt;
            vel.y -= 2200f * dt; vel *= 1f - 0.8f * dt;
            pos += vel * dt;
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.Euler(0, 0, spin * t);
            rt.localScale = new Vector3(Mathf.Cos(t * 9f), 1f, 1f); // flutter
            var c = img.color; c.a = Mathf.Clamp01((life - t) * 2f); img.color = c;
            yield return null;
        }
        Destroy(img.gameObject);
    }

    public void Flash(Color color, float duration = 0.35f)
    {
        var img = NewImage(Root, "Flash", JuiceSprites.White, color, Vector2.zero);
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        float a0 = color.a;
        StartCoroutine(Fade(img, a0, duration));
    }

    IEnumerator Fade(Image img, float a0, float duration)
    {
        yield return JuiceTween.Run(duration, p => { var c = img.color; c.a = a0 * (1f - p); img.color = c; });
        Destroy(img.gameObject);
    }

    public void Toast(string message, Color accent)
    {
        StartCoroutine(ToastCo(message, accent));
    }

    IEnumerator ToastCo(string message, Color accent)
    {
        var bg = NewImage(Root, "Toast", JuiceSprites.Pill, new Color(0.08f, 0.1f, 0.15f, 0.88f), new Vector2(560, 90));
        bg.type = Image.Type.Sliced;
        var rt = bg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        var rim = NewImage(rt, "Rim", JuiceSprites.Border, accent, Vector2.zero);
        rim.type = Image.Type.Sliced;
        rim.rectTransform.anchorMin = Vector2.zero; rim.rectTransform.anchorMax = Vector2.one;
        rim.rectTransform.offsetMin = rim.rectTransform.offsetMax = Vector2.zero;
        var star = NewImage(rt, "Star", JuiceSprites.Star, accent, new Vector2(56, 56));
        star.rectTransform.anchoredPosition = new Vector2(-220, 0);
        var txt = NewText(rt, message, 40, Color.white);
        txt.rectTransform.anchoredPosition = new Vector2(30, 0);
        txt.rectTransform.sizeDelta = new Vector2(440, 70);

        Vector2 hidden = new Vector2(0, 80), shown = new Vector2(0, -90);
        yield return JuiceTween.Run(0.4f, p => rt.anchoredPosition = Vector2.LerpUnclamped(hidden, shown, JuiceTween.EaseOutBack(p)));
        float t = 0;
        while (t < 1.8f) { t += Time.unscaledDeltaTime; star.rectTransform.localRotation = Quaternion.Euler(0, 0, t * 180f); yield return null; }
        yield return JuiceTween.Run(0.3f, p => rt.anchoredPosition = Vector2.Lerp(shown, hidden, JuiceTween.EaseInCubic(p)));
        Destroy(bg.gameObject);
    }

    public static IEnumerator Shake(RectTransform rt, float strength = 28f, float duration = 0.4f)
    {
        if (!rt) yield break;
        Vector2 basePos = rt.anchoredPosition;
        yield return JuiceTween.Run(duration, p =>
        {
            if (!rt) return;
            float k = (1f - p) * strength;
            rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(p * 60f) * k, Mathf.Cos(p * 47f) * k * 0.3f);
        });
        if (rt) rt.anchoredPosition = basePos;
    }

    public static IEnumerator Punch(Transform tr, float amount = 0.25f, float duration = 0.35f)
    {
        if (!tr) yield break;
        Vector3 b = tr.localScale;
        yield return JuiceTween.Run(duration, p =>
        {
            if (tr) tr.localScale = b * (1f + amount * Mathf.Sin(p * Mathf.PI) * (1f - p));
        });
        if (tr) tr.localScale = b;
    }
}

// Applies an outline to a TMP text the first time it is shown (see JuiceUI.SetOutline).
public class DeferredOutline : MonoBehaviour
{
    public float Width;
    public Color32 Color;

    void OnEnable()
    {
        var t = GetComponent<TMP_Text>();
        if (t) { t.outlineWidth = Width; t.outlineColor = Color; }
        Destroy(this);
    }
}
